namespace Wfc.Screens;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Audio;
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
  private bool _isPaused;

  [Dependency]
  public ISfxManager SfxManager => this.DependOn<ISfxManager>();
  [Dependency]
  public IMusicTrackManager MusicTrackManager => this.DependOn<IMusicTrackManager>();

  public void OnResolved() { }

  public override void _Ready() {
    this.WireNodes();
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

  private void OnBackButtonPressed() {
    SfxManager.StopAll();
    Resume();
    _pauseMenu.GoToMainMenu();
  }

  private void _on_ResumeButton2_pressed() {
    if (_isPaused) {
      Resume();
    }
  }

  private void _on_LevelSelectButton_pressed() {
    SfxManager.StopAll();
    Resume();
    _pauseMenu.GoToLevelSelectMenu();
  }
}
