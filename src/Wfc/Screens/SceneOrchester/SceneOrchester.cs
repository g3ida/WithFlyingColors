namespace Wfc.Screens;

using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Audio;
using Wfc.Core.Event;
using Wfc.Core.Persistence;
using Wfc.Entities.Ui;
using Wfc.Entities.World.Checkpoints;
using Wfc.Entities.World.Door;
using Wfc.Screens.Levels;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class SceneOrchester : Node2D {
  public override void _Notification(int what) {
    this.Notify(what);
    // Closing the window does not pass through a menu, so this is the run's only chance to be
    // written down: checkpoints bank progress, and everything since the last one - the gems in
    // the HUD, the door the player is standing at - would otherwise go with the process.
    if (what is (int)NotificationWMCloseRequest or (int)NotificationWMGoBackRequest) {
      _writeProgressToSlot();
    }
  }

  [Dependency]
  public IMenuManager MenuManager => this.DependOn<IMenuManager>();

  [Dependency]
  public ISaveManager SaveManager => this.DependOn<ISaveManager>();

  [Dependency]
  public IMusicTrackManager MusicTrackManager => this.DependOn<IMusicTrackManager>();

  // The id the level's Cutscene node matches start and end requests on. Unique to
  // the intro so it cannot collide with a cutscene the level itself is running.
  private const string INTRO_CUTSCENE_ID = "LevelIntro";

  [NodePath("LevelTitleCard")]
  private LevelTitleCard _titleCardNode = default!;

  GameLevel? _currentLevel = null;
  LevelId? _currentLevelId = null;
  // The level the swap will land on: set when a clear or a door entry starts the
  // cover, cleared once the swap has happened behind it.
  private LevelId? _pendingLevelId = null;
  // Set when the pending swap is a clear, so the cover callback knows to bank a
  // completion rather than a doorstep save. The gems ride along because the HUD
  // that knows them is freed with the cleared level before the write happens.
  private LevelId? _pendingClearedLevelId = null;
  private string[] _pendingClearedGems = [];
  // Set when the pending swap is a restart, which writes nothing: the slot already
  // describes this level, and a doorstep save here would push the resume point back
  // to the start of a level the player has got further into.
  private bool _pendingRestart;

  // The intro walks the player forward while the title fades over the scene; the
  // walk budget comes from the level itself.
  private bool _introActive;
  private float _introWalkTimeLeft;
  // The level whose door owes the player a ceremony, held from the moment its clear is
  // banked until the hub's own title has faded and there is something to watch.
  private LevelId? _celebrationLevelId;

  public override void _EnterTree() {
    base._EnterTree();
    ConnectSignals();
  }

  public override void _ExitTree() {
    base._ExitTree();
    DisconnectSignals();
    MusicTrackManager.Stop();
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    SetProcess(false);
    _titleCardNode.Covered += _onTitleCardCovered;
    _titleCardNode.TitleFinished += _onTitleFinished;
  }

  public void OnResolved() {
    var metaData = SaveManager.GetSlotMetaData();
    var decision = LevelStartPolicy.Choose(
      MenuManager.GetCurrentLevelId(),
      metaData?.LevelId,
      metaData?.Progress ?? 0,
      (metaData?.ClearedLevels.Count ?? 0) > 0,
      LevelDispatcher.LEVELS.First().Id
    );

    _startLevel(decision.LevelId, decision.ShouldRestoreSavedGame);
    // A level taken from its top gets the full entrance; a checkpoint resume drops
    // the player exactly where they left, where a scripted walk could shove them
    // off whatever they saved on.
    if (!decision.ShouldRestoreSavedGame) {
      _beginLevelIntro();
    }
  }

  private void _startLevel(LevelId levelId, bool restoreSavedGame, LevelId? levelJustLeft = null) {
    _currentLevelId = levelId;
    _currentLevel = _loadLevel(levelId);
    if (_currentLevel != null && restoreSavedGame) {
      SaveManager.LoadGame(GetTree(), _currentLevel.PlayerNode, _currentLevel.CameraNode);
    }
    if (levelId == LevelId.Hub) {
      _standAtHubDoor(levelJustLeft);
    }
  }

  // The hub is a room rather than a menu, so where the player is standing when it opens is
  // the whole of what it says to them: they step out of the door of the level they have just
  // left, and a run picked up from a save opens on the door that run is on.
  private void _standAtHubDoor(LevelId? levelJustLeft) {
    if (_currentLevel is not { } hub) {
      return;
    }
    IReadOnlySet<LevelId> clearedLevels = SaveManager.GetSlotMetaData()?.ClearedLevels ?? new HashSet<LevelId>();
    var doorLevelId = HubSpawnPolicy.DoorToStandAt(
      levelJustLeft,
      [.. LevelDispatcher.LEVELS.Select(level => level.Id)],
      clearedLevels
    );
    var door = hub.FindDescendants<Door>().FirstOrDefault(door => door.TargetLevel == doorLevelId);
    if (door == null) {
      return;
    }
    // Only the doorway is taken from the door: its own height is wherever the arch happens to
    // be drawn, while the level authored a spawn that is standing on the floor.
    var player = hub.PlayerNode;
    player.GlobalPosition = new Vector2(door.GlobalPosition.X, player.GlobalPosition.Y);
    hub.CameraNode.SnapTo(player.GlobalPosition);
  }

  private void ConnectSignals() {
    EventHandler.Instance.Events.PlayerDied += OnGameOver;
    EventHandler.Instance.Events.LevelCleared += OnLevelCleared;
    EventHandler.Instance.Events.CheckpointReached += _onCheckpointReached;
    EventHandler.Instance.Events.DoorEntered += _onDoorEntered;
    EventHandler.Instance.Events.LevelRestartRequested += _onLevelRestartRequested;
  }

  private void DisconnectSignals() {
    EventHandler.Instance.Events.PlayerDied -= OnGameOver;
    EventHandler.Instance.Events.LevelCleared -= OnLevelCleared;
    EventHandler.Instance.Events.CheckpointReached -= _onCheckpointReached;
    EventHandler.Instance.Events.DoorEntered -= _onDoorEntered;
    EventHandler.Instance.Events.LevelRestartRequested -= _onLevelRestartRequested;
  }

  // A checkpoint is the game's own statement that the run so far is worth keeping, so it is where
  // the slot gets written. Nothing wrote one before: the whole IPersistent machinery ran exactly
  // once, on an empty slot, and quitting lost the run.
  private void _onCheckpointReached(Vector2 _position, string _colorGroup) {
    // Deferred, because this node subscribes in its own _EnterTree - before the level it will
    // create even exists - so its handler runs ahead of the player's, the gems' and the camera's.
    // Writing here would persist the snapshots from the *previous* checkpoint. By the end of the
    // frame every IPersistent has recorded the current one.
    Callable.From(_writeProgressToSlot).CallDeferred();
  }

  private void _writeProgressToSlot() {
    if (_currentLevel == null || _currentLevelId == null) {
      return;
    }
    SaveManager.RecordProgress(GetTree(), _currentLevelId.Value, _checkpointProgressPercent(), _collectedGemGroups());
  }

  // What the HUD says the player is holding right now - the gems a reload of this
  // write would give back, which is exactly what the hub doors may show.
  private string[] _collectedGemGroups() =>
    _currentLevel == null
      ? []
      : [.. ColorUtils.COLOR_GROUPS.Where(_currentLevel.GemsHUDContainerNode.IsGemCollected)];

  // The share of the current level's checkpoints the player has passed. Scoped to the
  // level rather than the whole tree, so nothing outside it can dilute the count.
  private int _checkpointProgressPercent() {
    if (_currentLevel == null) {
      return 0;
    }
    var checkpoints = _currentLevel.FindDescendants<CheckpointArea>().ToList();
    if (checkpoints.Count == 0) {
      return 0;
    }
    var reached = checkpoints.Count(checkpoint => checkpoint.IsChecked);
    return (int)Mathf.Round(100f * reached / checkpoints.Count);
  }

  private static void OnGameOver() {
    EventHandler.Instance.EmitCheckpointLoaded();
  }

  private GameLevel? _loadLevel(LevelId levelId) {
    var level = LevelDispatcher.InstantiateLevel(levelId);
    if (level != null) {
      AddChild(level);
      level.Owner = this;
      // Input runs through later siblings first, and the title card must see - and
      // swallow - the pause key before the level's own pause menu does.
      MoveChild(_titleCardNode, GetChildCount() - 1);
    }
    else {
      GD.PrintErr($"Could not Instantiate level {levelId}");
    }
    return level;
  }

  private void OnLevelCleared() {
    if (_currentLevel == null || _currentLevelId == null) {
      GD.PushError("LevelCleared raised with no current level: there is nothing to advance from.");
      return;
    }

    var next = LevelDispatcher.NextLevel(_currentLevelId.Value);
    if (next == null) {
      // End of the chain: the cleared screen takes over, and the slot parks on the
      // finished level while the save still describes it.
      SaveManager.RecordLevelCleared(GetTree(), _currentLevelId.Value, null, _collectedGemGroups());
      _currentLevel.PauseMenuNode.NavigateToScreen(GameMenus.LEVEL_CLEAR_MENU);
      return;
    }

    // Any other clear walks back out to the hub: the next door is unlocked there
    // rather than the next level starting on its own.
    _pendingLevelId = LevelId.Hub;
    _pendingClearedLevelId = _currentLevelId;
    _pendingClearedGems = _collectedGemGroups();
    // Freeze play under the cover; the swap happens once it is opaque.
    GetTree().Paused = true;
    _titleCardNode.CoverForSwap();
  }

  // A door is a request to swap levels inside the game screen, exactly like the
  // clear path minus the completion: same cover, same save-after-swap.
  private void _onDoorEntered(int levelId) {
    if (_pendingLevelId != null) {
      return;
    }
    _pendingLevelId = (LevelId)levelId;
    _pendingClearedLevelId = null;
    _pendingClearedGems = [];
    _pendingRestart = false;
    GetTree().Paused = true;
    _titleCardNode.CoverForSwap();
  }

  // Starting over is the same swap with the level it lands on being the one it left,
  // so the level gets rebuilt from its scene rather than reset piece by piece: nothing
  // the run touched - a broken brick, a spent power-up - can survive it.
  private void _onLevelRestartRequested() {
    if (_pendingLevelId != null || _currentLevelId == null) {
      return;
    }
    _pendingLevelId = _currentLevelId;
    _pendingClearedLevelId = null;
    _pendingClearedGems = [];
    _pendingRestart = true;
    GetTree().Paused = true;
    _titleCardNode.CoverForSwap();
  }

  private void _onTitleCardCovered() {
    if (_pendingLevelId is not { } nextLevelId) {
      return;
    }
    _pendingLevelId = null;
    var clearedLevelId = _pendingClearedLevelId;
    var clearedGems = _pendingClearedGems;
    var isRestart = _pendingRestart;
    _pendingClearedLevelId = null;
    _pendingClearedGems = [];
    _pendingRestart = false;

    // Removal first: the old level's _ExitTree stops the music before the new one
    // starts its own track, and the persist group must hold only the new level's
    // nodes by the time the save below serializes it.
    var levelJustLeft = _currentLevelId;
    if (_currentLevel != null) {
      RemoveChild(_currentLevel);
      _currentLevel.QueueFree();
    }
    _startLevel(nextLevelId, restoreSavedGame: false, levelJustLeft);

    // Before the write, not after: the door hears the clear the moment it is banked, and a
    // door that has not been warned shows the gems there and then - under the cover, where
    // nobody sees them arrive.
    if (clearedLevelId is { } toCelebrate && nextLevelId == LevelId.Hub) {
      _celebrationLevelId = toCelebrate;
      _doorFor(toCelebrate)?.ExpectCelebration();
    }

    // Saved after the swap, so the slot is a coherent "standing at the start of the
    // next level" snapshot rather than a mix of two levels. A door entry is no
    // completion, but the doorstep is still where the run now stands: quitting here
    // must resume here.
    if (clearedLevelId is { } cleared) {
      SaveManager.RecordLevelCleared(GetTree(), cleared, nextLevelId, clearedGems);
    }
    else if (!isRestart) {
      SaveManager.RecordProgress(GetTree(), nextLevelId, 0);
    }

    // Play resumes under the lifting cover, but inside the intro cutscene: the
    // player is walked in while the title fades over the scene. If the window lost
    // focus during the cover, the pause menu legitimately owns the pause now and
    // keeps it.
    if (_currentLevel?.PauseMenuNode.IsPaused != true) {
      GetTree().Paused = false;
    }
    _beginLevelIntro();
  }

  private void _beginLevelIntro() {
    if (_currentLevel == null || _currentLevelId == null) {
      return;
    }
    var titleKey = LevelDispatcher.TitleKeyOf(_currentLevelId.Value);
    if (titleKey == null) {
      return;
    }
    _introActive = true;
    _introWalkTimeLeft = _currentLevel.IntroWalkTime;
    EventHandler.Instance.EmitCutsceneRequestStart(INTRO_CUTSCENE_ID);
    SetProcess(true);
    _titleCardNode.PresentTitle(titleKey.Value);
  }

  // Drives the intro walk, the same way the exit walks the player out: the input
  // lock belongs to the cutscene, so someone has to push.
  public override void _Process(double delta) {
    base._Process(delta);
    if (!_introActive) {
      SetProcess(false);
      return;
    }
    if (_introWalkTimeLeft <= 0f || _currentLevel == null) {
      return;
    }
    _introWalkTimeLeft -= (float)delta;
    var player = _currentLevel.PlayerNode;
    if (player != null && IsInstanceValid(player)) {
      player.SetMaxSpeed();
    }
  }

  private void _onTitleFinished() {
    if (!_introActive) {
      return;
    }
    _introActive = false;
    SetProcess(false);
    EventHandler.Instance.EmitCutsceneRequestEnd(INTRO_CUTSCENE_ID);
    _celebrateClearedDoor();
  }

  // Held back until the title is gone and nothing is over the scene: the clear was banked
  // while the cover was still down, and the door's whole point is being watched.
  private void _celebrateClearedDoor() {
    if (_celebrationLevelId is not { } celebrated) {
      return;
    }
    _celebrationLevelId = null;
    _doorFor(celebrated)?.Celebrate();
  }

  private Door? _doorFor(LevelId levelId) =>
    _currentLevel?.FindDescendants<Door>().FirstOrDefault(door => door.TargetLevel == levelId);
}
