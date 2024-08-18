namespace Wfc.Screens;

using Godot;
using Wfc.Screens.MenuManager;
using Wfc.Entities.Ui.Menubox;
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

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    SaveGame.Instance().Init();
    _currentSlotLabelNode.Text = $"Current slot: {SaveGame.Instance().currentSlotIndex + 1}";
  }

  public void ShowResetDataDialog() => _resetSlotDialogNode.ShowDialog();

  public override bool OnMenuButtonPressed(MenuButtons menu_button) {
    switch (menu_button) {
      case MenuButtons.QUIT:
        if (_screenState == MenuScreenState.ENTERED) {
          GetTree().Quit();
        }
        return true;
      case MenuButtons.PLAY:
        return true;
      case MenuButtons.STATS:
        NavigateToScreen(GameMenus.STATS_MENU);
        return true;
      case MenuButtons.SETTINGS:
        NavigateToScreen(GameMenus.SETTINGS_MENU);
        return true;
      case MenuButtons.BACK:
        _menuBoxNode.HideSubMenuIfNeeded();
        return true;
      default:
        return ProcessPlaySubMenus(menu_button);
    }
  }

  private bool ProcessPlaySubMenus(MenuButtons menuButton) {
    switch (menuButton) {
      case MenuButtons.NEW_GAME:
        if (SaveGame.Instance().DoesSlotHaveProgress(SaveGame.Instance().currentSlotIndex)) {
          _resetSlotDialogNode.ShowDialog();
        }
        else {
          NavigateToScreen(GameMenus.GAME);
          _menuBoxNode.HideSubMenuIfNeeded();
        }
        return true;
      case MenuButtons.CONTINUE_GAME:
        _menuBoxNode.HideSubMenuIfNeeded();
        NavigateToScreen(GameMenus.GAME);
        return true;
      case MenuButtons.SELECT_SLOT:
        _menuBoxNode.HideSubMenuIfNeeded();
        NavigateToScreen(GameMenus.SELECT_SLOT);
        return true;
      default:
        return false;
    }
  }

  private void OnResetSlotConfirmed() {
    SaveGame.Instance().RemoveSaveSlot(SaveGame.Instance().currentSlotIndex);
    _menuBoxNode.HideSubMenuIfNeeded();
    NavigateToScreen(GameMenus.GAME);
  }
}
