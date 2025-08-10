namespace Wfc.Screens;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core;
using Wfc.Core.Audio;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class PauseMenu : CanvasLayer {
  public override void _Notification(int what) => this.Notify(what);

  [NodePath("ScreenShaders")]
  private ScreenShaders _screenShaders = null!;
  [NodePath("PauseMenuImpl")]
  private PauseMenuImpl _pauseMenu = null!;
  [NodePath("PauseMenuImpl/CenterContainer/VBoxContainer/LevelSelectButton")]
  private Button _levelSelectButton = null!;
  [NodePath("PauseMenuImpl/CenterContainer/VBoxContainer/ResumeButton")]
  private Button _resumeButton = null!;
  [NodePath("PauseMenuImpl/CenterContainer/VBoxContainer/BackButton")]
  private Button _backButtonButton = null!;

  private bool _isPaused;

  [Dependency]
  public ISfxManager SfxManager => this.DependOn<ISfxManager>();
  [Dependency]
  public IMusicTrackManager MusicTrackManager => this.DependOn<IMusicTrackManager>();

  public void OnResolved() { }

  public override void _Ready() {
    this.WireNodes();
    _levelSelectButton.Pressed += _onLevelSelectButtonPressed;
    _resumeButton.Pressed += _onResumeButtonPressed;
    _backButtonButton.Pressed += _onBackButtonPressed;
  }

  public override void _Process(double delta) {
    if (Input.IsActionJustPressed("pause")) {
      if (_isPaused) {
        Resume();
      }
      else {
        PauseGame();
      }
    }
  }

  private void Resume() {
    SfxManager.ResumeAll();
    MusicTrackManager.SetPauseMenuEffect(false);
    _screenShaders.DisablePauseShader();
    _pauseMenu._Hide();
    _isPaused = false;
    GetTree().Paused = false;
    EventHandler.Instance.EmitPauseMenuExit();
  }

  private void PauseGame() {
    SfxManager.PauseAll();
    MusicTrackManager.SetPauseMenuEffect(true);
    _screenShaders.Call("ActivatePauseShader");
    _pauseMenu._Show();
    _isPaused = true;
    GetTree().Paused = true;
    EventHandler.Instance.EmitPauseMenuEnter();
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

  private void _onLevelSelectButtonPressed() {
    SfxManager.StopAll();
    Resume();
    _pauseMenu.GoToLevelSelectMenu();
  }

  public void NavigateToScreen(GameMenus menuScreen) {
    _pauseMenu.NavigateToScreen(menuScreen);
  }
}
