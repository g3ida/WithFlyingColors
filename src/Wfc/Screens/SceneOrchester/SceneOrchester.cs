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
    var isNewGame = (metaData == null) || (metaData.Progress == 0);

    // PackedScene? sceneResource = null;

    var levelId = MenuManager.GetCurrentLevelId();
    if (levelId != null) {
      _loadLevel((LevelId)levelId);
      // need to load level here
    }
    else if (isNewGame) {
      levelId = LevelDispatcher.LEVELS.First().Id;
      _loadLevel((LevelId)levelId);
    }
    else {
      _loadLevel(metaData!.LevelId);
      SaveManager.LoadGame(GetTree());
    }
  }

  private void ConnectSignals() {
    EventHandler.Instance.Connect(EventType.PlayerDied, new Callable(this, nameof(OnGameOver)));
    EventHandler.Instance.Connect(EventType.LevelCleared, new Callable(this, nameof(OnLevelCleared)));
  }

  private void DisconnectSignals() {
    EventHandler.Instance.Disconnect(EventType.PlayerDied, new Callable(this, nameof(OnGameOver)));
    EventHandler.Instance.Disconnect(EventType.LevelCleared, new Callable(this, nameof(OnLevelCleared)));
  }

  private void OnGameOver() {
    EventHandler.Instance.EmitCheckpointLoaded();
  }

  private void _loadLevel(LevelId levelId) {
    var level = LevelDispatcher.InstantiateLevel(levelId);
    if (level != null) {
      AddChild(level);
      level.Owner = this;
    }
    else {
      GD.PrintErr($"Could not Instantiate level {levelId}");
    }
  }

  private void OnLevelCleared() {
    // FIXME: uncomment this line after implementing PauseMenu in c#
    Global.Instance().PauseMenu.NavigateToScreen(GameMenus.LEVEL_CLEAR_MENU);
  }
}
