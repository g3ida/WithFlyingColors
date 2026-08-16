namespace Wfc.Entities.World.BrickBreaker;

using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Audio;
using Wfc.Core.Event;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Entities.World;
using Wfc.Entities.World.BrickBreaker.Powerups;
using Wfc.Entities.World.Camera;
using Wfc.Entities.World.Checkpoints;
using Wfc.Entities.World.Platforms;
using Wfc.Screens.Levels;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[Meta(typeof(IAutoNode))]
public partial class BrickBreaker : Node2D, IPersistent {
  public override void _Notification(int what) => this.Notify(what);
  private const float FACE_SEPARATOR_SCALE_FACTOR = 3.5f;
  private const int NUM_LEVELS = 2;
  private const float LEVELS_Y_GAP = 36 * 4;
  private const float LEVELS_WIN_GAP = 2.5f * LEVELS_Y_GAP;
  private const float BRICKS_TRANSLATION_Y_AMOUNT = 500.0f;

  [Dependency]
  public IMusicTrackManager MusicTrackManager => this.DependOn<IMusicTrackManager>();
  [Dependency]
  public IGameLevel GameLevel => this.DependOn<IGameLevel>();

  public void OnResolved() { }

  #region Nodes
  private BricksTileMap? BricksTileMapNode = null;
  [NodePath("DeathZone")]
  private Area2D _deathZoneNode = default!;
  [NodePath("BallsContainer")]
  public Node2D BallsContainer = default!;
  [NodePath("BallsContainer/BallSpawnPos")]
  private Marker2D _ballSpawnPosNode = default!;
  [NodePath("BricksContainer/BricksSpawnPos")]
  private Marker2D _bricksSpawnPosNode = default!;
  [NodePath("BricksContainer/LevelUpTimer")]
  private Timer _bricksTimerNode = default!;
  [NodePath("BricksContainer/BrickPowerUpHandler")]
  private IPowerUpHandler _bricksPowerUpHandler = default!;
  [NodePath("CheckpointArea")]
  private CheckpointArea _checkpointNode = default!;
  [NodePath("TriggerEnterArea")]
  private Area2D? _triggerEnterAreaNode = default!;
  [NodePath("SlidingFloor")]
  private Node2D _slidingFloorNode = default!;
  [NodePath("SlidingFloor/Slider")]
  private PlatformSlider _slidingFloorSliderNode = default!;
  [NodePath("ProtectionAreaSpawnerPosition")]
  public Node2D ProtectionAreaSpawnerPositionNode = default!;
  [NodePath("SlidingDoor/Slider")]
  private PlatformSlider _slidingDoorNode = default!;
  [NodePath("CameraLocalizer")]
  private CameraLocalizer _cameraLocalizerNode = default!;
  #endregion Nodes

  private BrickBreakerState _currentState = BrickBreakerState.STOPPED;
  private int _numBalls = 0;
  private int _currentLevel = 0;
  private Tween? _bricksMoveTweener;

  private sealed record SaveData(BrickBreakerState state = BrickBreakerState.STOPPED);
  private SaveData _saveData = new SaveData();

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
  }

  public BouncingBall SpawnBall(string color = "blue") {
    var bouncingBall = SceneHelpers.InstantiateNode<BouncingBall>();
    bouncingBall.DeathZone = _deathZoneNode;
    bouncingBall.ColorGroup = color;
    BallsContainer.CallDeferred(Node.MethodName.AddChild, bouncingBall);
    bouncingBall.CallDeferred(Node2D.MethodName.SetPosition, _ballSpawnPosNode.Position);
    bouncingBall.CallDeferred(Node.MethodName.SetOwner, BallsContainer);
    bouncingBall.CallDeferred(nameof(BouncingBall.SetColor), color);
    bouncingBall.GlobalPosition = _ballSpawnPosNode.GlobalPosition;
    _numBalls += 1;
    return bouncingBall;
  }

  ///
  private void RemoveBricks() {
    if (BricksTileMapNode != null) {
      BricksTileMapNode.bricksCleared -= _OnBricksCleared;
      BricksTileMapNode.levelCleared -= _OnLevelCleared;
      BricksTileMapNode.QueueFree();
      BricksTileMapNode = null;
    }
  }

  private BricksTileMap SpawnBricks(bool shouldInstanceBricks = true, bool shouldTranslateDown = true) {
    var bricks = SceneHelpers.InstantiateNode<BricksTileMap>();
    bricks.should_instance_bricks = shouldInstanceBricks;
    bricks.Position = _bricksSpawnPosNode.Position + (shouldTranslateDown ? Vector2.Up * BRICKS_TRANSLATION_Y_AMOUNT : Vector2.Zero);
    CallDeferred(Node.MethodName.AddChild, bricks);
    bricks.CallDeferred(Node.MethodName.SetOwner, this);
    if (shouldTranslateDown) {
      CreateBricksMoveTweener();
      CallDeferred(nameof(MoveBricksDownBy), BRICKS_TRANSLATION_Y_AMOUNT, 5.0f);
    }
    return bricks;
  }

  private void ConnectSignals() {
    EventHandler.Instance.Events.CheckpointReached += _OnCheckpointHit;
    EventHandler.Instance.Events.CheckpointLoaded += Reset;
    EventHandler.Instance.Events.PlayerDying += _OnPlayerDying;
    EventHandler.Instance.Events.BouncingBallRemoved += _OnBouncingBallRemoved;
  }

  private void DisconnectSignals() {
    EventHandler.Instance.Events.CheckpointReached -= _OnCheckpointHit;
    EventHandler.Instance.Events.CheckpointLoaded -= Reset;
    EventHandler.Instance.Events.PlayerDying -= _OnPlayerDying;
    EventHandler.Instance.Events.BouncingBallRemoved -= _OnBouncingBallRemoved;
  }

  public override void _EnterTree() {
    ConnectSignals();
  }

  public override void _ExitTree() {
    DisconnectSignals();
  }

  private void IncrementBallsSpeed() {
    foreach (Node2D b in BallsContainer.GetChildren()) {
      if (b is BouncingBall ball) {
        ball.IncrementSpeed();
      }
    }
  }

  private void RemoveBalls() {
    foreach (Node2D b in BallsContainer.GetChildren()) {
      if (b is BouncingBall) {
        b.QueueFree();
      }
    }
    _numBalls = 0;
  }

  private void Reset() {
    _currentState = _saveData.state;
    if (_currentState == BrickBreakerState.INIT_PLAYING) {
      MusicTrackManager.SetPitchScale(1);
      Play();
    }
    else if (_currentState == BrickBreakerState.WIN) {
      if (BricksTileMapNode == null) {
        BricksTileMapNode = SpawnBricks(false, false);
        BricksTileMapNode.Position += new Vector2(0, LEVELS_Y_GAP * NUM_LEVELS + LEVELS_WIN_GAP);
      }
    }
  }

  private BrickBreakerState GetSaveStateFromCurrentState() {
    if (_currentState == BrickBreakerState.LOSE || _currentState == BrickBreakerState.PLAYING) {
      return BrickBreakerState.INIT_PLAYING;
    }
    return _currentState;
  }

  private void _OnCheckpointHit(Vector2 _position, string _colorGroup) {
    _saveData = new SaveData(GetSaveStateFromCurrentState());
  }

  private void _OnPlayerDying(Node? _area, Vector2 _position, int _entityType) {
    if (_currentState == BrickBreakerState.PLAYING) {
      _currentState = BrickBreakerState.LOSE;
      Stop();
    }
  }

  private void Stop() {
    RemoveBalls();
    RemoveBricks();
    _bricksTimerNode.Stop();
  }

  private async void Play() {
    if (_currentState != BrickBreakerState.PLAYING) {
      _currentState = BrickBreakerState.PLAYING;
      _currentLevel = 0;
      // A round begins in an empty room. Losing clears it, but not everything that starts a round
      // is preceded by a loss - restarting from the pause menu puts the level back without anybody
      // dying - and a round that began over a round already in progress descended its wall straight
      // through the standing one, which then hung there for good.
      Stop();
      // Winning switches the spawner off for the celebration, and nothing else ever switched it
      // back on: a second round in the same session dropped no power-ups at all.
      _bricksPowerUpHandler.SetActive(true);
      BricksTileMapNode = SpawnBricks();
      EventHandler.Instance.EmitBrickBreakerStart();
      if (_bricksMoveTweener != null) {
        await ToSignal(_bricksMoveTweener, Tween.SignalName.Finished);
      }
      SpawnBall();
      _bricksTimerNode.Start();
      BricksTileMapNode.bricksCleared += _OnBricksCleared;
      BricksTileMapNode.levelCleared += _OnLevelCleared;
      GameLevel.PlayerNode.CurrentDefaultCornerScaleFactor = FACE_SEPARATOR_SCALE_FACTOR;
    }
  }

  private void _OnBouncingBallRemoved(Node2D _ball) {
    _numBalls -= 1;
    if (_numBalls <= 0) {
      EventHandler.Instance.EmitPlayerDying(_deathZoneNode, _ball.GlobalPosition, EntityType.BrickBreaker);
    }
  }

  private void _onTriggerEnterAreaBodyEntered(Node body) {
    if (body != GameLevel.PlayerNode)
      return;
    var isStarting = _currentState == BrickBreakerState.STOPPED;
    if (isStarting) {
      _currentState = BrickBreakerState.INIT_PLAYING;
      CallDeferred(nameof(Play));
      _slidingFloorSliderNode.StopSlider(false);
      MusicTrackManager.LoadTrack("brickBreaker");
      MusicTrackManager.PlayTrack("brickBreaker");
    }

    if (_currentState == BrickBreakerState.WIN) {
      ChangeCameraViewAfterWin();
    }

    _cameraLocalizerNode.ApplyLimitsToCamera();
    if (_triggerEnterAreaNode != null) {
      _triggerEnterAreaNode.QueueFree();
      _triggerEnterAreaNode = null;
    }

    if (isStarting) {
      // The arena's checkpoint sits below this trigger, so the lift carries the player across it
      // first, and everything it saved describes a room that had not been entered yet: a stopped
      // arena, a lift still on its loop and the level's own music. Dying in here then restored a
      // room with no game in it and no trigger left to start one. Record it again now that the
      // arena is running.
      EventHandler.Instance.EmitCheckpointReached(_checkpointNode.GlobalPosition, _checkpointNode.ColorGroup);
    }
  }

  private void _onLevelUpTimerTimeout() {
    _currentLevel += 1;
    if (_currentLevel == NUM_LEVELS) {
      _bricksTimerNode.Stop();
    }
    CreateBricksMoveTweener();
    MoveBricksDownBy(LEVELS_Y_GAP);
    MusicTrackManager.SetPitchScale(1 + (_currentLevel - 1) * 0.1f);
    IncrementBallsSpeed();
  }

  private void CreateBricksMoveTweener() {
    if (_bricksMoveTweener != null) {
      _bricksMoveTweener.Kill();
    }
    _bricksMoveTweener = CreateTween();
  }

  private void MoveBricksDownBy(float value, float duration = 0.25f) {
    if (BricksTileMapNode != null) {
      _bricksMoveTweener?.TweenProperty(
          BricksTileMapNode,
          "position:y",
          BricksTileMapNode.Position.Y + value,
          duration).From(BricksTileMapNode.Position.Y);
    }
  }

  private async void _OnBricksCleared() {
    if (_currentState == BrickBreakerState.PLAYING) {
      _currentState = BrickBreakerState.WIN;
      _bricksTimerNode.Stop();
      EventHandler.Instance.EmitBreakBreakerWin();
      CleanUpGame();
      CreateBricksMoveTweener();
      MoveBricksDownBy(LEVELS_WIN_GAP, 3.0f);
      if (_bricksMoveTweener != null) {
        await ToSignal(_bricksMoveTweener, Tween.SignalName.Finished);
      }
      MusicTrackManager.SetPitchScale(1);
      _slidingDoorNode.ResumeSlider();
      ChangeCameraViewAfterWin();
      // A checkpoint with no post to stand on. Clearing the arena is progress worth keeping -
      // Level1 has no checkpoint of its own between here and the piano - so the room records one
      // where the player actually is, facing the way they actually are.
      //
      // This used to go through a helper that newed up a CheckpointArea, never added it to the
      // tree and called its collision callback by hand: the position came back as the world
      // origin and the fabricated color group flipped the cube upside down, so every death for
      // the rest of the level teleported the player to (0, 0) rotated 180 degrees.
      var player = GameLevel.PlayerNode;
      EventHandler.Instance.EmitCheckpointReached(player.GlobalPosition, player.GroundColorGroup);
    }
  }

  private void CleanUpGame() {
    RemoveBalls();
    _bricksPowerUpHandler.SetActive(false);
    _bricksPowerUpHandler.RemoveActivePowerups();
    _bricksPowerUpHandler.RemoveFallingPowerups();
  }

  // The arena is done with, and the way on is up: the camera is handed back to the player and the
  // ceiling taken off the room they leave through.
  private void ChangeCameraViewAfterWin() {
    _cameraLocalizerNode.LimitedEdges = CameraEdges.Left | CameraEdges.Right | CameraEdges.Bottom;
    _cameraLocalizerNode.FreezeCamera = false;
    _cameraLocalizerNode.ApplyToCamera();
  }

  private void _OnLevelCleared(int level) {
    if (_currentState == BrickBreakerState.PLAYING) {
      if (_currentLevel != NUM_LEVELS && _currentLevel + 1 <= level && _bricksTimerNode.TimeLeft > 2.0f) {
        _bricksTimerNode.Stop();
        _bricksTimerNode.Start();
        _onLevelUpTimerTimeout();
      }
    }
  }

  public string GetSaveId() => this.GetPath();
  public string Save(ISerializer serializer) => serializer.Serialize(_saveData);
  public void Load(ISerializer serializer, string data) {
    var deserializedData = serializer.Deserialize<SaveData>(data);
    this._saveData = deserializedData ?? new SaveData();
    Reset();
  }
}
