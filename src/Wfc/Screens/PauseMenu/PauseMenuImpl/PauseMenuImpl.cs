namespace Wfc.Screens;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Localization;
using Wfc.Entities.Ui;
using Wfc.Screens;
using Wfc.Screens.Levels;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;

public partial class PauseMenuImpl : GameMenu {
  private List<PauseMenuBtn> buttons = null!;

  [NodePath("CenterContainer/VBoxContainer/ResumeButton")]
  private PauseMenuBtn _resumeButton = null!;
  [NodePath("CenterContainer/VBoxContainer/RestartCheckpointButton")]
  private PauseMenuBtn _restartCheckpointButton = null!;
  [NodePath("CenterContainer/VBoxContainer/RestartLevelButton")]
  private PauseMenuBtn _restartLevelButton = null!;
  [NodePath("CenterContainer/VBoxContainer/ReturnToHubButton")]
  private PauseMenuBtn _returnToHubButton = null!;
  [NodePath("CenterContainer/VBoxContainer/SettingsButton")]
  private PauseMenuBtn _settingsButton = null!;
  [NodePath("CenterContainer/VBoxContainer/BackButton")]
  private PauseMenuBtn _backButton = null!;


  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    buttons = new List<PauseMenuBtn> {
      _resumeButton,
      _restartCheckpointButton,
      _restartLevelButton,
      _returnToHubButton,
      _settingsButton,
      _backButton,
    };
    _refreshButtonTexts();
    HandleBackEvent = false;
  }

  // The buttons hold strings that were already translated when the overlay was
  // built, so the engine's own auto-translation has nothing left to redo once the
  // player picks another language.
  public override void _Notification(int what) {
    base._Notification(what);
    if (what == NotificationTranslationChanged && IsNodeReady()) {
      _refreshButtonTexts();
    }
  }

  private void _refreshButtonTexts() {
    _resumeButton.Text = LocalizationService.GetLocalizedString(TranslationKey.menu_button_resumeGame);
    _restartCheckpointButton.Text = LocalizationService.GetLocalizedString(TranslationKey.menu_button_restartCheckpoint);
    _restartLevelButton.Text = LocalizationService.GetLocalizedString(TranslationKey.menu_button_restartLevel);
    _returnToHubButton.Text = LocalizationService.GetLocalizedString(TranslationKey.menu_button_returnToHub);
    _settingsButton.Text = LocalizationService.GetLocalizedString(TranslationKey.menu_button_settings);
    _backButton.Text = LocalizationService.GetLocalizedString(TranslationKey.menu_button_mainMenu);
  }

  // Drops the entries that only mean something inside a level being played, so the
  // menu never offers an action the level it sits in cannot carry out. The buttons
  // start hidden and are only ever shown from this list, so leaving them out of it
  // keeps them off the screen and out of the focus chain.
  public void HideLevelOnlyActions() =>
      buttons.RemoveAll(button =>
          button == _restartCheckpointButton
          || button == _restartLevelButton
          || button == _returnToHubButton);

  public void HideReturnToHub() => buttons.Remove(_returnToHubButton);

  public void _Hide() {
    foreach (var button in buttons) {
      button.HideBtn();
    }
  }

  public void _Show() {
    buttons[0].GrabFocus();
    foreach (var button in buttons) {
      button.ShowBtn();
    }
  }

  public void GoToMainMenu() {
    NavigateToScreen(GameMenus.MAIN_MENU);
  }

  public void ReturnToHub() {
    // The hub is where levels are picked. It is another level under the same game
    // screen, so this rides the door-swap rail instead of a menu navigation - the
    // orchestrator covers the scene and swaps to the hub exactly as if a door had
    // been walked through.
    GameEvents.Instance.OnDoorEntered(LevelId.Hub);
  }

  public void RestartLevel() => GameEvents.Instance.OnLevelRestartRequested();

  // The same road back a death takes: every checkpoint-aware node listens for this
  // and puts itself back where the last checkpoint found it.
  public void RestartFromCheckpoint() => GameEvents.Instance.OnCheckpointLoaded();
}
