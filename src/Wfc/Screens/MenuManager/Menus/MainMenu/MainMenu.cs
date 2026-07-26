namespace Wfc.Screens.MenuManager.Menus.MainMenu;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Localization;
using Wfc.Core.Persistence;
using Wfc.Entities.Ui;
using Wfc.Entities.Ui.Menubox;
using Wfc.Entities.Ui.Slots;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class MainMenu : GameMenu {

  [NodePath("CurrentSlotLabel")]
  private Label _currentSlotLabelNode = null!;

  [NodePath("MenuBox")]
  private Menubox _menuBoxNode = null!;

  [NodePath("ResetDialogContainer")]
  private DialogContainer _resetSlotDialogNode = null!;

  public override void _EnterTree() {
    base._EnterTree();
  }
  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _refreshCurrentSlotLabel();
  }

  // The label holds a string that was already translated when the screen was built,
  // so the engine's own auto-translation has nothing left to redo once the player
  // picks another language.
  public override void _Notification(int what) {
    base._Notification(what);
    if (what == NotificationTranslationChanged && IsNodeReady()) {
      _refreshCurrentSlotLabel();
    }
  }

  private void _refreshCurrentSlotLabel() =>
    _currentSlotLabelNode.Text = SaveManager.GetCurrentSlotLine(LocalizationService);

  public void ShowResetDataDialog() => _resetSlotDialogNode.ShowDialog();

  public override bool OnMenuButtonPressed(MenuAction menuAction) {
    switch (menuAction) {
      case MenuAction.Quit:
        if (_screenState == MenuScreenState.Entered) {
          GetTree().Quit();
        }
        return true;
      case MenuAction.Play:
        return true;
      case MenuAction.GoToStats:
        NavigateToScreen(GameMenus.STATS_MENU);
        return true;
      case MenuAction.GoToSettings:
        NavigateToScreen(GameMenus.SETTINGS_MENU);
        return true;
      case MenuAction.GoBack:
        _menuBoxNode.HideSubMenuIfNeeded();
        return true;
      default:
        return ProcessPlaySubMenus(menuAction);
    }
  }

  private bool ProcessPlaySubMenus(MenuAction menuAction) {
    switch (menuAction) {
      case MenuAction.NewGame:
        if (SaveManager.GetSlotMetaData()?.Progress > 0) {
          _resetSlotDialogNode.ShowDialog();
        }
        else {
          NavigateToScreen(GameMenus.GAME);
          _menuBoxNode.HideSubMenuIfNeeded();
        }
        return true;
      case MenuAction.ContinueGame:
        _menuBoxNode.HideSubMenuIfNeeded();
        NavigateToScreen(GameMenus.GAME);
        return true;
      // The action the play sub-menu's slot button actually emits. This listened for
      // SelectSlot, which is what the select-slot screen reports when a slot has been
      // chosen on it, so the button here answered to nothing.
      case MenuAction.GoToSlotSelect:
        _menuBoxNode.HideSubMenuIfNeeded();
        NavigateToScreen(GameMenus.SELECT_SLOT);
        return true;
      default:
        return false;
    }
  }

  private void OnResetSlotConfirmed() {
    if (SaveManager.HasSelectedSlot()) {
      var slotIndex = SaveManager.GetSelectedSlotIndex();
      SaveManager.RemoveSaveSlot(slotIndex);
      // Wiping the selected slot clears the selection, but the player is starting a
      // new game in that very slot: keep it selected so the first save doesn't
      // silently land in slot 0.
      SaveManager.SelectSlot(slotIndex);
    }
    _menuBoxNode.HideSubMenuIfNeeded();
    NavigateToScreen(GameMenus.GAME);
  }
}
