namespace Wfc.Core.Input;

using Godot;

public interface IInputManager {
  public enum Action {
    MoveLeft = 0,
    MoveRight = 1,
    Jump = 2,
    RotateLeft = 3,
    RotateRight = 4,
    Pause = 5,
    Dash = 6,
    Down = 7,
    UIConfirm = 8,
    UICancel = 9,
    UILeft = 10,
    UIRight = 11,
    UIUp = 12,
    UIDown = 13,
    UIHome = 14,
    UITabNext = 15,
    UITabPrevious = 16
  }
  public bool IsPressed(Action action);
  public bool IsJustReleased(Action action);
  public bool IsJustPressed(Action action);
  public bool IsEventActionJustPressed(Action action, InputEvent @event);
}
