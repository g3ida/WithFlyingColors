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

  // A release is answered once and then forgotten, which is the closest a fake with no frame of
  // its own gets to an engine that answers it for the frame the button came up in. Answering it
  // never, as this used to, hid the jump cut from every test that has ever run.
  //
  // Pressing drops one nobody asked about: the only reader of a release is short-circuited on a
  // timer, so an unread one would otherwise sit here and be answered on the far side of the next
  // press - cutting a jump the player is still holding down.
  private readonly HashSet<IInputManager.Action> _released = [];

  // Holds the action down until Release is called. The menus read the action rather
  // than the event, so the event a test passes alongside this only has to be non-null.
  public void Press(IInputManager.Action action) {
    _released.Remove(action);
    _pressed.Add(action);
  }

  public void Release(IInputManager.Action action) {
    if (_pressed.Remove(action)) {
      _released.Add(action);
    }
  }

  public void ReleaseAll() {
    _released.UnionWith(_pressed);
    _pressed.Clear();
  }

  public bool IsEventActionJustPressed(IInputManager.Action action, InputEvent @event) => _pressed.Contains(action);

  public bool IsJustPressed(IInputManager.Action action) => _pressed.Contains(action);

  public bool IsJustReleased(IInputManager.Action action) => _released.Remove(action);

  public bool IsPressed(IInputManager.Action action) => _pressed.Contains(action);
}
