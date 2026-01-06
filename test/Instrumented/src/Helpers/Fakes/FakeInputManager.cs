namespace Wfc.test.instrumented.Helpers.Fakes;

using Godot;
using Wfc.Core.Input;

sealed class FakeInputManager : IInputManager {
  public bool IsEventActionJustPressed(IInputManager.Action action, InputEvent @event) => throw new System.NotImplementedException();
  public bool IsJustPressed(IInputManager.Action action) => false;
  public bool IsJustReleased(IInputManager.Action action) => false;
  public bool IsPressed(IInputManager.Action action) => false;
}
