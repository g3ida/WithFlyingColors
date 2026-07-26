namespace Wfc.Screens;

using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.Screens.MenuManager;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class LevelClearedMenu : GameMenu {
  public override void _Ready() {
    base._Ready();
    // The screen owns cancel itself: the default back navigation would return to the
    // previous menu, which is the level the player just finished.
    HandleBackEvent = false;
  }

  public override void _Input(InputEvent @event) {
    base._Input(@event);
    // Either action dismisses the screen. Going through the input manager rather than
    // testing for InputEventKey covers the gamepad, and drops key releases and the
    // auto-repeat echoes that used to fire this twice per press.
    if (InputManager.IsEventActionJustPressed(IInputManager.Action.UIConfirm, @event) ||
        InputManager.IsEventActionJustPressed(IInputManager.Action.UICancel, @event)) {
      EventHandler.EmitMenuActionPressed(MenuAction.ExitClearedLevel);
    }
  }

  public override bool OnMenuButtonPressed(MenuAction menuAction) {
    if (menuAction == MenuAction.ExitClearedLevel) {
      NavigateToScreen(GameMenus.MAIN_MENU);
      return true;
    }
    return false;
  }
}
