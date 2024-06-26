using Godot;
using System;
using Wfc.Entities.Ui.Menubox;

public partial class MainMenu : GameMenu {
  private Label currentSlotLabelNode;
  private Menubox menuBoxNode;
  private DialogContainer resetSlotDialogNode;

  public override void _Ready() {
    base._Ready();
    menuBoxNode = GetNode<Menubox>("MenuBox");
    resetSlotDialogNode = GetNode<DialogContainer>("ResetDialogContainer");
    currentSlotLabelNode = GetNode<Label>("CurrentSlotLabel");

    SaveGame.Instance().Init();

    currentSlotLabelNode.Text = $"Current slot: {SaveGame.Instance().currentSlotIndex + 1}";
  }

  public void ShowResetDataDialog() {
    resetSlotDialogNode.ShowDialog();
  }

  public override bool on_menu_button_pressed(MenuButtons menuButton) {
    switch (menuButton) {
      case MenuButtons.QUIT:
        if (screenState == MenuScreenState.ENTERED) {
          GetTree().Quit();
        }
        return true;
      case MenuButtons.PLAY:
        return true;
      case MenuButtons.STATS:
        NavigateToScreen(MenuManager.Menus.STATS_MENU);
        return true;
      case MenuButtons.SETTINGS:
        NavigateToScreen(MenuManager.Menus.SETTINGS_MENU);
        return true;
      case MenuButtons.BACK:
        menuBoxNode.HideSubMenuIfNeeded();
        return true;
      default:
        return ProcessPlaySubMenus(menuButton);
    }
  }

  private bool ProcessPlaySubMenus(MenuButtons menuButton) {
    switch (menuButton) {
      case MenuButtons.NEW_GAME:
        if (SaveGame.Instance().DoesSlotHaveProgress(SaveGame.Instance().currentSlotIndex)) {
          resetSlotDialogNode.ShowDialog();
        }
        else {
          NavigateToScreen(MenuManager.Menus.GAME);
          menuBoxNode.HideSubMenuIfNeeded();
        }
        return true;
      case MenuButtons.CONTINUE_GAME:
        menuBoxNode.HideSubMenuIfNeeded();
        NavigateToScreen(MenuManager.Menus.GAME);
        return true;
      case MenuButtons.SELECT_SLOT:
        menuBoxNode.HideSubMenuIfNeeded();
        NavigateToScreen(MenuManager.Menus.SELECT_SLOT);
        return true;
      default:
        return false;
    }
  }

  private void _on_ResetSlotDialog_confirmed() {
    SaveGame.Instance().RemoveSaveSlot(SaveGame.Instance().currentSlotIndex);
    menuBoxNode.HideSubMenuIfNeeded();
    NavigateToScreen(MenuManager.Menus.GAME);
  }
}
