namespace Wfc.Screens.MenuManager.Menus.MainMenu;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Persistence;
using Wfc.Entities.Ui.Menubox;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class MainMenu : GameMenu {

  [NodePath("MenuBox")]
  private Menubox _menuBoxNode = null!;

  public override void _EnterTree() {
    base._EnterTree();
  }
  public override void _Ready() {
    base._Ready();
    this.WireNodes();
  }

  public override bool OnMenuButtonPressed(MenuAction menuAction) {
    switch (menuAction) {
      case MenuAction.Quit:
        GetTree().Quit();
        return true;
      case MenuAction.Play:
        // With every slot empty the box skipped its sub-menu, so Play is the whole
        // request: start a fresh game in the first slot without asking which.
        if (SaveManager.HasNoSaves()) {
          StartNewGameInSlot(0);
        }
        return true;
      case MenuAction.GoToCredits:
        NavigateToScreen(GameMenus.CREDITS_MENU);
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
      // Continue is the zero-friction path: it resumes the game most recently played,
      // whichever slot that was. The button only exists when the query has an answer.
      case MenuAction.ContinueGame:
        if (SaveManager.MostRecentlyPlayedSlotIndex() is { } slotIndex) {
          SaveManager.SelectSlot(slotIndex);
          _menuBoxNode.HideSubMenuIfNeeded();
          NavigateToScreen(GameMenus.GAME);
        }
        return true;
      // Choosing where a new game lives - and confirming a wipe if that spot is
      // taken - is the slot picker's business, so both of these just open it in the
      // right mode.
      case MenuAction.NewGame:
        MenuManager.SetSlotPickerMode(SlotPickerMode.NewGame);
        _menuBoxNode.HideSubMenuIfNeeded();
        NavigateToScreen(GameMenus.SELECT_SLOT);
        return true;
      case MenuAction.LoadGame:
        MenuManager.SetSlotPickerMode(SlotPickerMode.Load);
        _menuBoxNode.HideSubMenuIfNeeded();
        NavigateToScreen(GameMenus.SELECT_SLOT);
        return true;
      default:
        return false;
    }
  }
}
