namespace Wfc.Screens;

using System;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Audio;
using Wfc.Core.Event;
using Wfc.Core.Persistence;
using Wfc.Entities.World.Checkpoints;
using Wfc.Screens.Levels;
using Wfc.Screens.MenuManager;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class SceneOrchester : Node2D {
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public IMenuManager MenuManager => this.DependOn<IMenuManager>();

  [Dependency]
  public ISaveManager SaveManager => this.DependOn<ISaveManager>();

  [Dependency]
  public IMusicTrackManager MusicTrackManager => this.DependOn<IMusicTrackManager>();

  GameLevel? _currentLevel = null;
  LevelId? _currentLevelId = null;

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
    SetProcess(false);
  }

  public void OnResolved() {
    var metaData = SaveManager.GetSlotMetaData();
    var decision = LevelStartPolicy.Choose(
      MenuManager.GetCurrentLevelId(),
      metaData?.LevelId,
      metaData?.Progress ?? 0,
      LevelDispatcher.LEVELS.First().Id
    );

    _currentLevelId = decision.LevelId;
    _currentLevel = _loadLevel(decision.LevelId);
    if (_currentLevel != null && decision.ShouldRestoreSavedGame) {
      SaveManager.LoadGame(GetTree(), _currentLevel.PlayerNode, _currentLevel.CameraNode);
    }
  }

  private void ConnectSignals() {
    EventHandler.Instance.Events.PlayerDied += OnGameOver;
    EventHandler.Instance.Events.LevelCleared += OnLevelCleared;
    EventHandler.Instance.Events.CheckpointReached += _onCheckpointReached;
  }

  private void DisconnectSignals() {
    EventHandler.Instance.Events.PlayerDied -= OnGameOver;
    EventHandler.Instance.Events.LevelCleared -= OnLevelCleared;
    EventHandler.Instance.Events.CheckpointReached -= _onCheckpointReached;
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
    SaveManager.RecordProgress(GetTree(), _currentLevelId.Value, _checkpointProgressPercent());
  }

  // The share of the level's checkpoints the player has passed.
  private int _checkpointProgressPercent() {
    var checkpoints = GetTree().GetNodesInGroup(IPersistent.PERSISTENT_GROUP_NAME).OfType<CheckpointArea>().ToList();
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
    }
    else {
      GD.PrintErr($"Could not Instantiate level {levelId}");
    }
    return level;
  }

  private void OnLevelCleared() {
    if (_currentLevel == null) {
      GD.PushError("LevelCleared raised with no current level: the level clear screen cannot be shown.");
      return;
    }
    _currentLevel.PauseMenuNode.NavigateToScreen(GameMenus.LEVEL_CLEAR_MENU);
  }
}
