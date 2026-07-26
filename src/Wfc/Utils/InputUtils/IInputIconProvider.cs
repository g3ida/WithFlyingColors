namespace Wfc.Utils;

using System.Collections.Generic;
using Godot;

// Resolves the on-screen glyph for an input binding on one kind of device.
// Hint UI talks to this instead of reaching into the device specific icon
// helpers, so it never has to branch on the active controller type itself.
public interface IInputIconProvider {
  // The glyph for the first binding in the list this device can represent, or
  // null when the action isn't bound on this device at all.
  InputGlyph? GetGlyph(IEnumerable<InputEvent> events);

  // The glyph standing for "any direction". Menu navigation isn't rebindable,
  // so it has no InputMap events to resolve.
  InputGlyph? GetNavigationGlyph();
}
