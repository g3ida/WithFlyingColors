namespace Wfc.test.instrumented.Helpers.Fakes;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Input;

// An input manager a test can drive directly, so a screen can be asked what it does
// about a press without an actual device.
//
// IsEventActionJustPressed used to throw, which was harmless while nothing tested the
// menus and fatal the moment one did: it is the call every menu _Input now makes.
public sealed class FakeInputManager : IInputManager {
  private readonly HashSet<IInputManager.Action> _pressed = [];

  // Holds the action down until Release is called. The menus read the action rather
  // than the event, so the event a test passes alongside this only has to be non-null.
  public void Press(IInputManager.Action action) => _pressed.Add(action);

  public void Release(IInputManager.Action action) => _pressed.Remove(action);

  public void ReleaseAll() => _pressed.Clear();

  public bool IsEventActionJustPressed(IInputManager.Action action, InputEvent @event) => _pressed.Contains(action);

  public bool IsJustPressed(IInputManager.Action action) => _pressed.Contains(action);

  public bool IsJustReleased(IInputManager.Action action) => false;

  public bool IsPressed(IInputManager.Action action) => _pressed.Contains(action);
}
