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
      EventHandler.EmitMenuActionPressed(MenuAction.ExitClearedLevel);
    }
  }

  public override bool OnMenuButtonPressed(MenuAction menuAction) {
    base.OnMenuButtonPressed(menuAction);
    if (menuAction == MenuAction.ExitClearedLevel) {
      NavigateToScreen(GameMenus.MAIN_MENU);
      return true;
    }
    return false;
  }
}
