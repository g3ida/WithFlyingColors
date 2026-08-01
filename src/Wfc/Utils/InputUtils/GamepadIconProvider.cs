namespace Wfc.Utils;

using System.Collections.Generic;
using Godot;

// Resolves input glyphs from gamepad bindings, drawn as controller button art.
// Buttons and axes GamepadIconHelper has no icon for fall back to a key cap
// carrying their name, so the hint still reads as a button rather than as
// loose text.
//
// OnDarkBackground picks the inverted art: menus draw their hints on light
// panels, while hints painted into the game world sit on the level itself.
public sealed class GamepadIconProvider : IInputIconProvider {
  private readonly bool _onDarkBackground;

  public GamepadIconProvider(bool onDarkBackground = false) {
    _onDarkBackground = onDarkBackground;
  }

  public InputGlyph? GetGlyph(IEnumerable<InputEvent> events) {
    var button = InputUtils.GetFirstJoypadButtonEventFromActionList(events);
    if (button != null) {
      var icon = GamepadIconHelper.GetButtonIcon(button.ButtonIndex, onDarkBackground: _onDarkBackground);
      return icon != null
          ? InputGlyph.Icon(icon)
          : KeyboardIconHelper.GetTextCap(InputUtils.GetJoyButtonName(button.ButtonIndex));
    }

    var axis = InputUtils.GetFirstJoypadAxisEventFromActionList(events);
    if (axis != null) {
      var icon = GamepadIconHelper.GetAxisIcon(axis.Axis, axis.AxisValue, onDarkBackground: _onDarkBackground);
      return icon != null
          ? InputGlyph.Icon(icon)
          : KeyboardIconHelper.GetTextCap(InputUtils.GetJoyAxisName(axis.Axis, axis.AxisValue));
    }

    return null;
  }

  public InputGlyph? GetNavigationGlyph() {
    var icon = GamepadIconHelper.GetDirectionalPadIcon(onDarkBackground: _onDarkBackground);
    return icon == null ? null : InputGlyph.Icon(icon);
  }
}
