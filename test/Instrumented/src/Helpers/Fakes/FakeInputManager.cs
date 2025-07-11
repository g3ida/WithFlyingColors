namespace Wfc.test.instrumented.Helpers.Fakes;

using Wfc.Core.Input;

class FakeInputManager : IInputManager {
  public bool IsJustPressed(IInputManager.Action action) => false;
  public bool IsJustReleased(IInputManager.Action action) => false;
  public bool IsPressed(IInputManager.Action action) => false;
}
