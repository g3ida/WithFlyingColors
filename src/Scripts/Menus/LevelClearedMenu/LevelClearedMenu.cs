using Godot;
using Wfc.Screens.MenuManager;
using Wfc.Screens;
using Wfc.Core.Event;

public partial class LevelClearedMenu : GameMenu {
  public override void _Ready() {
    base._Ready();
  }

  public override void _Input(InputEvent ev) {
    base._Input(ev);
    // Handle input based on input type
    if (ev is InputEventKey) {
      EventHandler.Emit(EventType.MenuButtonPressed, (int)MenuButtons.EXIT_LEVEL_CLEAR);
    }
  }

  public override bool OnMenuButtonPressed(MenuButtons menuButton) {
    base.OnMenuButtonPressed(menuButton);
    if (menuButton == MenuButtons.EXIT_LEVEL_CLEAR) {
      NavigateToScreen(GameMenus.MAIN_MENU);
      return true;
    }
    return false;
  }
}
