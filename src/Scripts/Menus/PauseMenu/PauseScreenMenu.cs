using Godot;
using System;
using Wfc.Core.Event;

public partial class PauseScreenMenu : CanvasLayer {
  private ScreenShaders screenShaders;
  private PauseMenu pauseMenu;
  private bool isPaused = false;

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
    AudioManager.Instance().ResumeAllSfx();
    AudioManager.Instance().MusicTrackManager.SetPauseMenuEffect(false);
    screenShaders.DisablePauseShader();
    pauseMenu._Hide();
    isPaused = false;
    GetTree().Paused = false;
    Event.Instance.EmitPauseMenuExit();
  }

  private void PauseGame() {
    AudioManager.Instance().PauseAllSfx();
    AudioManager.Instance().MusicTrackManager.SetPauseMenuEffect(true);
    screenShaders.Call("ActivatePauseShader");
    pauseMenu._Show();
    isPaused = true;
    GetTree().Paused = true;
    Event.Instance.EmitPauseMenuEnter();
  }

  private void OnBackButtonPressed() {
    AudioManager.Instance().StopAllSfx();
    Resume();
    pauseMenu.GoToMainMenu();
  }

  private void _on_ResumeButton2_pressed() {
    if (isPaused) {
      Resume();
    }
  }

  private void _on_LevelSelectButton_pressed() {
    AudioManager.Instance().StopAllSfx();
    Resume();
    pauseMenu.GoToLevelSelectMenu();
  }
}
