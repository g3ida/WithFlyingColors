namespace Wfc.Core.Input;

using System.Collections.Generic;
using Godot;

// The navigation a menu reads, with the stick made to behave like a button.
//
// A stick is a position rather than a press. Pushed quickly it lands several
// above-deadzone events in a single frame, and pushed gently it chatters across the
// deadzone; either way every one of those events reads as a fresh press and the menu
// moves as many rows. A direction therefore engages once past ENGAGE_STRENGTH and only
// re-arms once the stick has fallen back to RELEASE_STRENGTH, so one push is one step
// however the axis got there. Buttons and keys are already discrete and go through
// untouched.
//
// Both thresholds are strength as the action reports it, which is how far the axis has
// come from the deadzone rather than from centre. Keeping the pair of them clear of the
// deadzone is what leaves a band for the stick to wobble in without stepping twice.
public sealed class UINavigationInput {
  private const float ENGAGE_STRENGTH = 0.5f;
  private const float RELEASE_STRENGTH = 0.2f;

  private readonly IInputManager _inputManager;
  private readonly HashSet<IInputManager.Action> _engaged = [];

  public UINavigationInput(IInputManager inputManager) {
    _inputManager = inputManager;
  }

  // True on the one event that should move the menu a step.
  public bool IsJustPressed(IInputManager.Action action, InputEvent @event) {
    if (@event is not InputEventJoypadMotion) {
      return _inputManager.IsEventActionJustPressed(action, @event);
    }

    var name = InputManager.Actions[action];
    if (!@event.IsAction(name)) {
      return false;
    }

    var strength = @event.GetActionStrength(name);
    if (strength <= RELEASE_STRENGTH) {
      _engaged.Remove(action);
      return false;
    }
    if (strength < ENGAGE_STRENGTH) {
      return false;
    }
    return _engaged.Add(action);
  }

  // Re-arming has to keep up even while something else owns the screen, or a stick
  // let go of under a dialog is still engaged when the menu comes back and the next
  // push moves nothing.
  public void ObserveMotion(InputEvent @event) {
    if (@event is not InputEventJoypadMotion || _engaged.Count == 0) {
      return;
    }

    _engaged.RemoveWhere(action => {
      var name = InputManager.Actions[action];
      return @event.IsAction(name) && @event.GetActionStrength(name) <= RELEASE_STRENGTH;
    });
  }

  public void Reset() => _engaged.Clear();
}
