using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Audio;
using Wfc.Core.Event;
using EventHandler = Wfc.Core.Event.EventHandler;

[Meta(typeof(IAutoNode))]
public partial class BrickBreaker : Node2D {
  public override void _Notification(int what) => this.Notify(what);
  private const float FACE_SEPARATOR_SCALE_FACTOR = 3.5f;
  private const int NUM_LEVELS = 2;
  private const float LEVELS_Y_GAP = 36 * 4;
  private const float LEVELS_WIN_GAP = 2.5f * LEVELS_Y_GAP;
  private const float BRICKS_TRANSLATION_Y_AMOUNT = 500.0f;

  private PackedScene BricksTileMap = (PackedScene)GD.Load("res://Assets/Scenes/BrickBreaker/BricksTileMap.tscn");
  private PackedScene BouncingBallScene = (PackedScene)GD.Load("res://Assets/Scenes/BrickBreaker/BouncingBall.tscn");

  [Dependency]
  public IMusicTrackManager MusicTrackManager => this.DependOn<IMusicTrackManager>();

  public void OnResolved() { }

  private Area2D DeathZoneNode;
  private BricksTileMap? BricksTileMapNode = null;
  public Node2D BallsContainer;
  private Marker2D BallsSpawnPos;
  private Marker2D BallSpawnPosNode;
  private Marker2D BricksSpawnPosNode;
  private Timer BricksTimerNode;
  private IPowerUpHandler bricksPowerUpHandler;
  private Area2D Checkpoint;
  private Area2D? TriggerEnterAreaNode;
  private Node2D SlidingFloorNode;
  private SlidingPlatform SlidingFloorSliderNode;
  public Node2D ProtectionAreaSpawnerPositionNode;
  private SlidingPlatform SlidingDoorNode;
  private CameraLocalizer CameraLocalizerNode;

  private BrickBreakerState current_state = BrickBreakerState.STOPPED;

  private int num_balls = 0;
  private int current_level = 0;

  private Dictionary<string, object> save_data = new Dictionary<string, object>
    {
        {"state", (int)BrickBreakerState.STOPPED}
    };

  private Tween bricksMoveTweener;

  public override void _Ready() {
    DeathZoneNode = GetNode<Area2D>("DeathZone");
    BallsContainer = GetNode<Node2D>("BallsContainer");
    BallsSpawnPos = GetNode<Marker2D>("BallsContainer/BallSpawnPos");
    BallSpawnPosNode = GetNode<Marker2D>("BallsContainer/BallSpawnPos");
    BricksSpawnPosNode = GetNode<Marker2D>("BricksContainer/BricksSpawnPos");
    BricksTimerNode = GetNode<Timer>("BricksContainer/LevelUpTimer");
    bricksPowerUpHandler = GetNode<IPowerUpHandler>("BricksContainer/BrickPowerUpHandler");
    Checkpoint = GetNode<Area2D>("CheckpointArea");
    TriggerEnterAreaNode = GetNode<Area2D>("TriggerEnterArea");
    SlidingFloorNode = GetNode<Node2D>("SlidingFloor");
    SlidingFloorSliderNode = GetNode<SlidingPlatform>("SlidingFloor/SlidingPlatform");
    ProtectionAreaSpawnerPositionNode = GetNode<Node2D>("ProtectionAreaSpawnerPosition");
    SlidingDoorNode = GetNode<SlidingPlatform>("SlidingDoor/SlidingPlatform");
    CameraLocalizerNode = GetNode<CameraLocalizer>("CameraLocalizer");
  }

  public Node2D SpawnBall(string color = "blue") {
    var bouncingBall = BouncingBallScene.Instantiate<BouncingBall>();
    bouncingBall.DeathZone = DeathZoneNode;
    bouncingBall.color_group = color;
    BallsContainer.CallDeferred(Node.MethodName.AddChild, bouncingBall);
    bouncingBall.CallDeferred(Node2D.MethodName.SetPosition, BallSpawnPosNode.Position);
    bouncingBall.CallDeferred(Node.MethodName.SetOwner, BallsContainer);
    bouncingBall.CallDeferred(nameof(BouncingBall.SetColor), color);
    bouncingBall.GlobalPosition = BallsSpawnPos.GlobalPosition;
    num_balls += 1;
    return bouncingBall;
  }

  private void RemoveBricks() {
    if (BricksTileMapNode != null) {
      BricksTileMapNode.Disconnect(
        nameof(BricksTileMapNode.bricks_cleared),
        new Callable(this, nameof(_OnBricksCleared))
      );
      BricksTileMapNode.Disconnect(
        nameof(BricksTileMapNode.level_cleared),
        new Callable(this, nameof(_OnLevelCleared))
      );
      BricksTileMapNode.QueueFree();
      BricksTileMapNode = null;
    }
  }

  private BricksTileMap SpawnBricks(bool shouldInstanceBricks = true, bool shouldTranslateDown = true) {
    var bricks = BricksTileMap.Instantiate<BricksTileMap>();
    bricks.should_instance_bricks = shouldInstanceBricks;
    bricks.Position = BricksSpawnPosNode.Position + (shouldTranslateDown ? Vector2.Up * BRICKS_TRANSLATION_Y_AMOUNT : Vector2.Zero);
    CallDeferred(Node.MethodName.AddChild, bricks);
    bricks.CallDeferred(Node.MethodName.SetOwner, this);
    if (shouldTranslateDown) {
      CreateBricksMoveTweener();
      CallDeferred(nameof(MoveBricksDownBy), BRICKS_TRANSLATION_Y_AMOUNT, 5.0f);
    }
    return bricks;
  }

  private void ConnectSignals() {
    EventHandler.Instance.Connect(EventType.CheckpointReached, new Callable(this, nameof(_OnCheckpointHit)));
    EventHandler.Instance.Connect(EventType.CheckpointLoaded, new Callable(this, nameof(reset)));
    EventHandler.Instance.Connect(EventType.PlayerDying, new Callable(this, nameof(_OnPlayerDying)));
    EventHandler.Instance.Connect(EventType.BouncingBallRemoved, new Callable(this, nameof(_OnBouncingBallRemoved)));
  }

  private void DisconnectSignals() {
    EventHandler.Instance.Disconnect(EventType.CheckpointReached, new Callable(this, nameof(_OnCheckpointHit)));
    EventHandler.Instance.Disconnect(EventType.CheckpointLoaded, new Callable(this, nameof(reset)));
    EventHandler.Instance.Disconnect(EventType.PlayerDying, new Callable(this, nameof(_OnPlayerDying)));
    EventHandler.Instance.Disconnect(EventType.BouncingBallRemoved, new Callable(this, nameof(_OnBouncingBallRemoved)));
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
    num_balls = 0;
  }

  public Dictionary<string, object> save() {
    return save_data;
  }

  private void reset() {
    current_state = (BrickBreakerState)Helpers.ParseSaveDataInt(save_data, "state");
    if (current_state == BrickBreakerState.INIT_PLAYING) {
      MusicTrackManager.SetPitchScale(1);
      Play();
    }
    else if (current_state == BrickBreakerState.WIN) {
      if (BricksTileMapNode == null) {
        BricksTileMapNode = SpawnBricks(false, false);
        BricksTileMapNode.Position += new Vector2(0, LEVELS_Y_GAP * NUM_LEVELS + LEVELS_WIN_GAP);
      }
    }
  }

  private BrickBreakerState GetSaveStateFromCurrentState() {
    if (current_state == BrickBreakerState.LOSE || current_state == BrickBreakerState.PLAYING) {
      return BrickBreakerState.INIT_PLAYING;
    }
    return current_state;
  }

  private void _OnCheckpointHit(Node _checkpoint) {
    save_data["state"] = (int)GetSaveStateFromCurrentState();
    if (Checkpoint == _checkpoint) {
      // nothing to do for now
    }
  }

  private void _OnPlayerDying(Node? _area, Vector2 _position, int _entityType) {
    if (current_state == BrickBreakerState.PLAYING) {
      current_state = BrickBreakerState.LOSE;
      Stop();
    }
  }

  private void Stop() {
    RemoveBalls();
    RemoveBricks();
    BricksTimerNode.Stop();
  }

  private async void Play() {
    if (current_state != BrickBreakerState.PLAYING) {
      current_state = BrickBreakerState.PLAYING;
      current_level = 0;
      BricksTileMapNode = SpawnBricks();
      EventHandler.Instance.EmitBrickBreakerStart();
      await ToSignal(bricksMoveTweener, Tween.SignalName.Finished);
      SpawnBall();
      BricksTimerNode.Start();
      BricksTileMapNode.Connect(
        nameof(BricksTileMapNode.bricks_cleared),
        new Callable(this, nameof(_OnBricksCleared))
      );
      BricksTileMapNode.Connect(
        nameof(BricksTileMapNode.level_cleared),
        new Callable(this, nameof(_OnLevelCleared))
      );
      Global.Instance().Player.CurrentDefaultCornerScaleFactor = FACE_SEPARATOR_SCALE_FACTOR;
    }
  }

  private void _OnBouncingBallRemoved(Node2D _ball) {
    num_balls -= 1;
    if (num_balls <= 0) {
      EventHandler.Instance.EmitPlayerDying(DeathZoneNode, _ball.GlobalPosition, Constants.EntityType.BRICK_BREAKER);
    }
  }

  private void _on_TriggerEnterArea_body_entered(Node body) {
    if (body != Global.Instance().Player)
      return;
    if (current_state == BrickBreakerState.STOPPED) {
      CallDeferred("Play");
      SlidingFloorSliderNode.SetLooping(false);
      SlidingFloorSliderNode.StopSlider(false);
      MusicTrackManager.LoadTrack("brickBreaker");
      MusicTrackManager.PlayTrack("brickBreaker");
    }

    if (current_state == BrickBreakerState.WIN) {
      ChangeCameraViewAfterWin();
    }

    if (TriggerEnterAreaNode != null) {
      TriggerEnterAreaNode.QueueFree();
      TriggerEnterAreaNode = null;
    }
  }

  private void _on_LevelUpTimer_timeout() {
    current_level += 1;
    if (current_level == NUM_LEVELS) {
      BricksTimerNode.Stop();
    }
    CreateBricksMoveTweener();
    MoveBricksDownBy(LEVELS_Y_GAP);
    MusicTrackManager.SetPitchScale(1 + (current_level - 1) * 0.1f);
    IncrementBallsSpeed();
  }

  private void CreateBricksMoveTweener() {
    if (bricksMoveTweener != null) {
      bricksMoveTweener.Kill();
    }
    bricksMoveTweener = CreateTween();
  }

  private void MoveBricksDownBy(float value, float duration = 0.25f) {
    bricksMoveTweener.TweenProperty(
        BricksTileMapNode,
        "position:y",
        BricksTileMapNode.Position.Y + value,
        duration).From(BricksTileMapNode.Position.Y);
  }

  private async void _OnBricksCleared() {
    if (current_state == BrickBreakerState.PLAYING) {
      current_state = BrickBreakerState.WIN;
      BricksTimerNode.Stop();
      EventHandler.Instance.EmitBreakBreakerWin();
      CleanUpGame();
      CreateBricksMoveTweener();
      MoveBricksDownBy(LEVELS_WIN_GAP, 3.0f);
      await ToSignal(bricksMoveTweener, Tween.SignalName.Finished);
      MusicTrackManager.SetPitchScale(1);
      SlidingDoorNode.ResumeSlider();
      ChangeCameraViewAfterWin();
      Helpers.TriggerFunctionalCheckpoint();
    }
  }

  private void CleanUpGame() {
    RemoveBalls();
    bricksPowerUpHandler.SetActive(false);
    bricksPowerUpHandler.RemoveActivePowerups();
    bricksPowerUpHandler.RemoveFallingPowerups();
  }

  private void ChangeCameraViewAfterWin() {
    CameraLocalizerNode.position_clipping_mode = CameraLimit.LIMIT_ALL_BUT_TOP;
    CameraLocalizerNode.full_viewport_drag_margin = false;
    CameraLocalizerNode.SetCameraLimits();
    CameraLocalizerNode.ApplyCameraChanges();
  }

  private void _OnLevelCleared(int level) {
    if (current_state == BrickBreakerState.PLAYING) {
      if (current_level != NUM_LEVELS && current_level + 1 <= level && BricksTimerNode.TimeLeft > 2.0f) {
        BricksTimerNode.Stop();
        BricksTimerNode.Start();
        _on_LevelUpTimer_timeout();
      }
    }
  }

  public void load(Dictionary<string, object> save_data) {
    this.save_data = save_data;
    this.reset();
  }
}
