using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using System;
using Wfc.Core.Event;
using Wfc.Screens.MenuManager;

[Meta(typeof(IAutoNode))]
public partial class SceneOrchester : Node2D {
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public IMenuManager MenuManager => this.DependOn<IMenuManager>();

  public override void _EnterTree() {
    base._EnterTree();
    ConnectSignals();
  }

  public override void _ExitTree() {
    base._ExitTree();
    DisconnectSignals();
    AudioManager.Instance().MusicTrackManager.Stop();
  }

  public override void _Ready() {
    base._Ready();
    SetProcess(false);
  }

  public void OnResolved() {
    var metaData = SaveGame.Instance().GetCurrentSlotMetaData();
    bool isNewGame = (metaData == null) || (Convert.ToSingle(metaData["progress"]) <= 0.0f);

    PackedScene sceneResource = null;
    if (!string.IsNullOrEmpty(MenuManager.GetCurrentLevelScenePath())) {
      sceneResource = GD.Load<PackedScene>(MenuManager.GetCurrentLevelScenePath());
      SetupSceneGame(sceneResource, false);
    }
    else if (isNewGame) {
      sceneResource = GD.Load<PackedScene>(MenuScenes.START_LEVEL_MENU_SCENE);
      SetupSceneGame(sceneResource, true);
    }
    else {
      sceneResource = GD.Load<PackedScene>(metaData["scene_path"].ToString());
      SetupSceneGame(sceneResource, true);
    }
  }

  private void ConnectSignals() {
    Event.Instance.Connect(EventType.PlayerDied, new Callable(this, nameof(OnGameOver)));
    Event.Instance.Connect(EventType.LevelCleared, new Callable(this, nameof(OnLevelCleared)));
  }

  private void DisconnectSignals() {
    Event.Instance.Disconnect(EventType.PlayerDied, new Callable(this, nameof(OnGameOver)));
    Event.Instance.Disconnect(EventType.LevelCleared, new Callable(this, nameof(OnLevelCleared)));
  }

  private void OnGameOver() {
    Event.Instance.EmitCheckpointLoaded();
  }

  private void SetupSceneGame(PackedScene sceneResource, bool tryLoad) {
    var sceneInstance = sceneResource.Instantiate();
    AddChild(sceneInstance);
    sceneInstance.Owner = this;

    if (tryLoad) {
      SaveGame.Instance().CallDeferred(nameof(SaveGame.LoadIfNeeded));
    }
  }

  private void OnLevelCleared() {
    // FIXME: uncomment this line after implementing PauseMenu in c#
    Global.Instance().PauseMenu.NavigateToScreen(GameMenus.LEVEL_CLEAR_MENU);
  }
}
