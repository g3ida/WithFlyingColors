namespace Wfc.Core.Input;

using System.Collections.Generic;

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
    { IInputManager.Action.UICancel, "ui_accept" },
    { IInputManager.Action.UIConfirm, "ui_cancel" },
    { IInputManager.Action.UIHome, "ui_home" },
  };
  public bool IsJustPressed(IInputManager.Action action) => Godot.Input.IsActionJustPressed(Actions[action]);
  public bool IsJustReleased(IInputManager.Action action) => Godot.Input.IsActionJustReleased(Actions[action]);
  public bool IsPressed(IInputManager.Action action) => Godot.Input.IsActionPressed(Actions[action]);
}
