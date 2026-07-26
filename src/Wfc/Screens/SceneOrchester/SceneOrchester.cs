namespace Wfc.Screens;

using System;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Audio;
using Wfc.Core.Event;
using Wfc.Core.Persistence;
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

    _currentLevel = _loadLevel(decision.LevelId);
    if (_currentLevel != null && decision.ShouldRestoreSavedGame) {
      SaveManager.LoadGame(GetTree(), _currentLevel.PlayerNode, _currentLevel.CameraNode);
    }
  }

  private void ConnectSignals() {
    EventHandler.Instance.Events.PlayerDied += OnGameOver;
    EventHandler.Instance.Events.LevelCleared += OnLevelCleared;
  }

  private void DisconnectSignals() {
    EventHandler.Instance.Events.PlayerDied -= OnGameOver;
    EventHandler.Instance.Events.LevelCleared -= OnLevelCleared;
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
