using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Audio;
using EventHandler = Wfc.Core.Event.EventHandler;

[Meta(typeof(IAutoNode))]
public partial class PauseScreenMenu : CanvasLayer {
  public override void _Notification(int what) => this.Notify(what);
  private ScreenShaders screenShaders;
  private PauseMenu pauseMenu;
  private bool isPaused = false;

  [Dependency]
  public ISfxManager SfxManager => this.DependOn<ISfxManager>();

  [Dependency]
  public IMusicTrackManager MusicTrackManager => this.DependOn<IMusicTrackManager>();

  public void OnResolved() { }

  public override void _Ready() {
    screenShaders = GetNode<ScreenShaders>("ScreenShaders");
    pauseMenu = GetNode<PauseMenu>("PauseMenu");
  }

  public override void _Process(double delta) {
    if (Input.IsActionJustPressed("pause")) {
      if (isPaused) {
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
    screenShaders.DisablePauseShader();
    pauseMenu._Hide();
    isPaused = false;
    GetTree().Paused = false;
    EventHandler.Instance.EmitPauseMenuExit();
  }

  private void PauseGame() {
    SfxManager.PauseAll();
    MusicTrackManager.SetPauseMenuEffect(true);
    screenShaders.Call("ActivatePauseShader");
    pauseMenu._Show();
    isPaused = true;
    GetTree().Paused = true;
    EventHandler.Instance.EmitPauseMenuEnter();
  }

  private void OnBackButtonPressed() {
    SfxManager.StopAll();
    Resume();
    pauseMenu.GoToMainMenu();
  }

  private void _on_ResumeButton2_pressed() {
    if (isPaused) {
      Resume();
    }
  }

  private void _on_LevelSelectButton_pressed() {
    SfxManager.StopAll();
    Resume();
    pauseMenu.GoToLevelSelectMenu();
  }
}
