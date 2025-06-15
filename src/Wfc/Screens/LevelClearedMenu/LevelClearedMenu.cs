namespace Wfc.Screens;

using Godot;
using Wfc.Core.Event;

using Wfc.Screens.MenuManager;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class LevelClearedMenu : GameMenu {
  public override void _Ready() {
    base._Ready();
  }

  public override void _Input(InputEvent @event) {
    base._Input(@event);
    // Handle input based on input type
    if (@event is InputEventKey) {
      EventHandler.Emit(EventType.MenuButtonPressed, (int)MenuButtons.EXIT_LEVEL_CLEAR);
    }
  }

  public override bool OnMenuButtonPressed(MenuButtons menu_button) {
    base.OnMenuButtonPressed(menu_button);
    if (menu_button == MenuButtons.EXIT_LEVEL_CLEAR) {
      NavigateToScreen(GameMenus.MAIN_MENU);
      return true;
    }
    return false;
  }
}
