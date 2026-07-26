namespace Wfc.Utils;

using System.Collections.Generic;
using Godot;

// Resolves input glyphs from keyboard bindings, drawn as key cap sprites.
public sealed class KeyboardIconProvider : IInputIconProvider {
  public InputGlyph? GetGlyph(IEnumerable<InputEvent> events) {
    var key = InputUtils.GetFirstKeyKeyboardEventFromActionList(events);
    return key == null ? null : KeyboardIconHelper.GetKeyIcon(key.Keycode);
  }

  // The arrow keys stand in for the d-pad on a keyboard, and they ship as a
  // single cluster sprite.
  public InputGlyph? GetNavigationGlyph() {
    var icon = KeyboardIconHelper.GetArrowKeysIcon();
    return icon == null ? null : InputGlyph.Icon(icon);
  }
}
