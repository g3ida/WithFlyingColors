namespace Wfc.Screens;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core;
using Wfc.Core.Audio;
using Wfc.Core.Input;
using Wfc.Core.Ui;
using Wfc.Entities.Ui.InputHint;
using Wfc.Screens.Levels;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class PauseMenu : CanvasLayer {
  public override void _Notification(int what) {
    this.Notify(what);

    // Alt-tabbing away is not the player choosing to play on: the level would go
    // on running behind whatever they switched to, with nobody watching it.
    //
    // The window's notification, not the application's, which the display server
    // only sends once a debounce has gone by - too late to stop the cube walking
    // into a saw, and not sent at all to a player who alt-tabs straight back.
    if (what == NotificationWMWindowFocusOut && !_isPaused) {
      PauseGame();
    }
  }

  [NodePath("ScreenShaders")]
  private ScreenShaders _screenShaders = null!;
  [NodePath("PauseMenuImpl")]
  private PauseMenuImpl _pauseMenu = null!;
  [NodePath("PauseMenuImpl/CenterContainer/VBoxContainer/ReturnToHubButton")]
  private Button _returnToHubButton = null!;
  [NodePath("PauseMenuImpl/CenterContainer/VBoxContainer/RestartLevelButton")]
  private Button _restartLevelButton = null!;
  [NodePath("PauseMenuImpl/CenterContainer/VBoxContainer/RestartCheckpointButton")]
  private Button _restartCheckpointButton = null!;
  [NodePath("PauseMenuImpl/CenterContainer/VBoxContainer/ResumeButton")]
  private Button _resumeButton = null!;
  [NodePath("PauseMenuImpl/CenterContainer/VBoxContainer/SettingsButton")]
  private Button _settingsButton = null!;
  [NodePath("PauseMenuImpl/CenterContainer/VBoxContainer/BackButton")]
  private Button _backButtonButton = null!;
  [NodePath("InputHintBar")]
  private InputHintBar _inputHintBar = null!;

  // Built the first time the player asks for it rather than with the overlay. The
  // whole settings screen is a few hundred nodes, and the overlay is rebuilt with
  // every level: instanced here it was the single largest cost of starting one,
  // paid on every restart for a screen most runs never open.
  private PauseSettingsMenu? _pauseSettingsMenu;

  private bool _isPaused;

  // Whether this menu currently owns the tree's pause. The orchestrator asks before
  // unpausing after a level swap, so an auto-pause that fired during the title card
  // (the window losing focus) is not silently undone.
  public bool IsPaused => _isPaused;

  [Dependency]
  public ISfxManager SfxManager => this.DependOn<ISfxManager>();
  [Dependency]
  public IMusicTrackManager MusicTrackManager => this.DependOn<IMusicTrackManager>();
  [Dependency]
  public IInputManager InputManager => this.DependOn<IInputManager>();
  [Dependency]
  public IModalStack ModalStack => this.DependOn<IModalStack>();
  [Dependency]
  public IPauseOwnership PauseOwnership => this.DependOn<IPauseOwnership>();

  public void OnResolved() { }

  // The level this menu belongs to is freed while it is still paused whenever the player
  // restarts or walks out from the pause menu, and a claim nobody is left to release would
  // hold the game still for the rest of the run.
  public override void _ExitTree() {
    base._ExitTree();
    if (_isPaused) {
      _isPaused = false;
      PauseOwnership.Release(this);
    }
  }

  public override void _Ready() {
    this.WireNodes();
    _returnToHubButton.Pressed += _onReturnToHubButtonPressed;
    _restartLevelButton.Pressed += _onRestartLevelButtonPressed;
    _restartCheckpointButton.Pressed += _onRestartCheckpointButtonPressed;
    _resumeButton.Pressed += _onResumeButtonPressed;
    _settingsButton.Pressed += _onSettingsButtonPressed;
    _backButtonButton.Pressed += _onBackButtonPressed;
  }

  // Event-based and through the input manager, matching the menus: polling the raw
  // action name every frame both bypassed the rebindable action table and repeated
  // for each event delivered in the frame the key went down.
  public override void _Input(InputEvent @event) {
    // A key capture or a dialog inside the settings view holds the screen; the
    // pause key belongs to it until it lets go.
    if (ModalStack.IsAnyOpen) {
      return;
    }

    // While the settings view is up, backing out returns to the pause buttons
    // rather than to the game, and only if the view agrees to close.
    if (_pauseSettingsMenu is { IsOpen: true }) {
      if (InputManager.IsEventActionJustPressed(IInputManager.Action.Pause, @event)
          || InputManager.IsEventActionJustPressed(IInputManager.Action.UICancel, @event)) {
        _closeSettings();
        GetViewport().SetInputAsHandled();
      }
      return;
    }

    if (!InputManager.IsEventActionJustPressed(IInputManager.Action.Pause, @event)) {
      return;
    }

    if (_isPaused) {
      Resume();
    }
    else {
      PauseGame();
    }
    GetViewport().SetInputAsHandled();
  }

  private void Resume() {
    SfxManager.ResumeAll();
    MusicTrackManager.SetPauseMenuEffect(false);
    _screenShaders.DisablePauseShader();
    _pauseMenu._Hide();
    _inputHintBar.Exit();
    _isPaused = false;
    PauseOwnership.Release(this);
    GameEvents.Instance.OnPauseMenuExited();
  }

  private void PauseGame() {
    SfxManager.PauseAll();
    MusicTrackManager.SetPauseMenuEffect(true);
    _screenShaders.ActivatePauseShader();
    _pauseMenu._Show();
    _inputHintBar.Enter();
    _isPaused = true;
    PauseOwnership.Claim(this);
    GameEvents.Instance.OnPauseMenuEntered();
  }

  private void _onBackButtonPressed() {
    SfxManager.StopAll();
    Resume();
    _pauseMenu.GoToMainMenu();
  }

  private void _onResumeButtonPressed() {
    if (_isPaused) {
      Resume();
    }
  }

  // The settings view replaces the buttons inside the same overlay: the game
  // stays paused behind it and the pause hint bar hands over to the view's own.
  private void _onSettingsButtonPressed() {
    _pauseMenu._Hide();
    _inputHintBar.Exit();
    _settingsView().Open();
  }

  private void _closeSettings() {
    if (_pauseSettingsMenu?.TryClose() == true) {
      _pauseMenu._Show();
      _inputHintBar.Enter();
    }
  }

  // Kept once built, so a player going in and out of the settings pays for it once
  // per level. It takes the hint bar's place among the children because the overlay
  // draws its layers in order and the view belongs under the bar, not over it.
  private PauseSettingsMenu _settingsView() {
    if (_pauseSettingsMenu != null) {
      return _pauseSettingsMenu;
    }
    _pauseSettingsMenu = SceneHelpers.InstantiateNode<PauseSettingsMenu>();
    AddChild(_pauseSettingsMenu);
    MoveChild(_pauseSettingsMenu, _inputHintBar.GetIndex());
    return _pauseSettingsMenu;
  }

  private void _onReturnToHubButtonPressed() {
    SfxManager.StopAll();
    Resume();
    _pauseMenu.ReturnToHub();
  }

  // Both restarts unpause first: the orchestrator takes the pause back for itself
  // while the cover is down, and the checkpoint reset needs the level running again
  // to play out.
  private void _onRestartLevelButtonPressed() {
    SfxManager.StopAll();
    Resume();
    _pauseMenu.RestartLevel();
  }

  private void _onRestartCheckpointButtonPressed() {
    SfxManager.StopAll();
    Resume();
    _pauseMenu.RestartFromCheckpoint();
  }

  // Told by the level it belongs to which level that is, so the menu can leave out
  // what that level has no use for. The hub is where levels are picked: there is
  // nothing to restart there and nowhere to go back to.
  public void ConfigureForLevel(LevelId levelId) {
    if (levelId == LevelId.Hub) {
      _pauseMenu.HideLevelOnlyActions();
    }
  }

  public void NavigateToScreen(GameMenus menuScreen) {
    _pauseMenu.NavigateToScreen(menuScreen);
  }
}
