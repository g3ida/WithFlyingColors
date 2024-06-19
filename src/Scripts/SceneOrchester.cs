using Godot;
using System;

public partial class SceneOrchester : Node2D {
  public override void _EnterTree() {
    ConnectSignals();
  }

  public override void _ExitTree() {
    DisconnectSignals();
    AudioManager.Instance().MusicTrackManager.Stop();
  }

  public override void _Ready() {
    SetProcess(false);
    var metaData = SaveGame.Instance().GetCurrentSlotMetaData();
    bool isNewGame = (metaData == null) || (Convert.ToSingle(metaData["progress"]) <= 0.0f);
    PackedScene sceneResource = null;

    if (!string.IsNullOrEmpty(MenuManager.Instance().levelScenePath)) {
      sceneResource = GD.Load<PackedScene>(MenuManager.Instance().levelScenePath);
      SetupSceneGame(sceneResource, false);
    }
    else if (isNewGame) {
      sceneResource = GD.Load<PackedScene>(MenuManager.START_LEVEL_MENU_SCENE);
      SetupSceneGame(sceneResource, true);
    }
    else {
      sceneResource = GD.Load<PackedScene>(metaData["scene_path"].ToString());
      SetupSceneGame(sceneResource, true);
    }
  }

  private void ConnectSignals() {
    Event.Instance.Connect("player_died", new Callable(this, nameof(OnGameOver)));
    Event.Instance.Connect("level_cleared", new Callable(this, nameof(OnLevelCleared)));
  }

  private void DisconnectSignals() {
    Event.Instance.Disconnect("player_died", new Callable(this, nameof(OnGameOver)));
    Event.Instance.Disconnect("level_cleared", new Callable(this, nameof(OnLevelCleared)));
  }

  private void OnGameOver() {
    Event.Instance.EmitSignal("checkpoint_loaded");
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
    Global.Instance().PauseMenu.NavigateToScreen(MenuManager.Menus.LEVEL_CLEAR_MENU);
  }
}
