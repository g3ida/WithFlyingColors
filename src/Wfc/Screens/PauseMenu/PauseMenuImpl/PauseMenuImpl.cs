namespace Wfc.Screens;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Localization;
using Wfc.Entities.Ui;
using Wfc.Screens;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;

public partial class PauseMenuImpl : GameMenu {
  private List<PauseMenuBtn> buttons = null!;

  [NodePath("CenterContainer/VBoxContainer/ResumeButton")]
  private PauseMenuBtn _resumeButton = null!;
  [NodePath("CenterContainer/VBoxContainer/LevelSelectButton")]
  private PauseMenuBtn _levelSelectButton = null!;
  [NodePath("CenterContainer/VBoxContainer/BackButton")]
  private PauseMenuBtn _backButton = null!;


  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    buttons = new List<PauseMenuBtn> { _resumeButton, _levelSelectButton, _backButton };
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
    _levelSelectButton.Text = LocalizationService.GetLocalizedString(TranslationKey.menu_button_levelSelection);
    _backButton.Text = LocalizationService.GetLocalizedString(TranslationKey.menu_button_mainMenu);
  }

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

  public void GoToLevelSelectMenu() {
    NavigateToScreen(GameMenus.LEVEL_SELECT_MENU);
  }
}
