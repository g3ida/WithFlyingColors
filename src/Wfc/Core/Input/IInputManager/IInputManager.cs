namespace Wfc.Core.Input;

public interface IInputManager {
  public enum Action {
    MoveLeft,
    MoveRight,
    Jump,
    RotateLeft,
    RotateRight,
    Pause,
    Dash,
    Down
  }
  public bool IsPressed(Action action);
  public bool IsJustReleased(Action action);
  public bool IsJustPressed(Action action);
}
