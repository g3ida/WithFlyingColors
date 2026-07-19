namespace Wfc.Core.Input;

using System.Collections.Generic;
using Godot;

public class InputManager : IInputManager {

  public static readonly Dictionary<IInputManager.Action, string> Actions = new Dictionary<IInputManager.Action, string> {
    { IInputManager.Action.MoveLeft, "move_left" },
    { IInputManager.Action.MoveRight, "move_right" },
    { IInputManager.Action.Jump, "jump" },
    { IInputManager.Action.RotateLeft, "rotate_left" },
    { IInputManager.Action.RotateRight, "rotate_right" },
    { IInputManager.Action.Pause, "pause" },
    { IInputManager.Action.Dash, "dash" },
    { IInputManager.Action.Down, "down" },
    { IInputManager.Action.UILeft, "ui_left" },
    { IInputManager.Action.UIRight, "ui_right" },
    { IInputManager.Action.UIUp, "ui_up" },
    { IInputManager.Action.UIDown, "ui_down" },
    { IInputManager.Action.UICancel, "ui_cancel" },
    { IInputManager.Action.UIConfirm, "ui_accept" },
    { IInputManager.Action.UIHome, "ui_home" },
    { IInputManager.Action.UITabNext, "ui_tab_next" },
    { IInputManager.Action.UITabPrevious, "ui_tab_prev" }
  };
  public bool IsJustPressed(IInputManager.Action action) => Godot.Input.IsActionJustPressed(Actions[action]);
  public bool IsJustReleased(IInputManager.Action action) => Godot.Input.IsActionJustReleased(Actions[action]);
  public bool IsPressed(IInputManager.Action action) => Godot.Input.IsActionPressed(Actions[action]);

  public bool IsEventActionJustPressed(IInputManager.Action action, InputEvent @event) =>
    @event.IsAction(Actions[action]) && IsJustPressed(action) && !@event.IsEcho();
}
