namespace Wfc.Utils;

using System.Collections.Generic;
using Godot;

// Helper class for mapping keyboard keys to their key cap sprites.
// The mirror of GamepadIconHelper: keys that a real keyboard draws as a symbol
// (enter, shift, the arrow cluster) have dedicated art, and every other key
// reuses the blank cap with its name overlaid on top.
public static class KeyboardIconHelper {
  private const string KeyboardIconPath = "res://Assets/Sprites/controller/keyboard/";

  // Godot gives character keys the keycode of the character they type, so a key
  // in this range is drawn as that character, the way the physical cap prints
  // it. Space starts the range and is the one member with nothing to draw.
  private const int FirstPrintableKeycode = (int)Key.Space + 1;
  private const int LastPrintableKeycode = 126; // '~'

  // arrows.png packs the four arrow keys into a 3x2 grid whose top-left and
  // top-right cells are empty. The caps touch, so the regions are contiguous.
  private static readonly Dictionary<Key, Rect2> ArrowRegions = new() {
    [Key.Up] = new Rect2(54, 0, 54, 55),
    [Key.Left] = new Rect2(0, 54, 54, 56),
    [Key.Down] = new Rect2(54, 54, 54, 56),
    [Key.Right] = new Rect2(107, 54, 54, 56),
  };

  private static readonly Dictionary<Key, AtlasTexture> _arrowIcons = new();

  // Gets the glyph for a keyboard key: either dedicated art, or the blank cap
  // plus the text to overlay on it.
  public static InputGlyph? GetKeyIcon(Key key) {
    if (ArrowRegions.ContainsKey(key)) {
      var arrow = GetArrowIcon(key);
      return arrow == null ? null : InputGlyph.Icon(arrow);
    }

    var iconPath = GetKeyIconPath(key);
    if (!string.IsNullOrEmpty(iconPath)) {
      var icon = GD.Load<Texture2D>(iconPath);
      return icon == null ? null : InputGlyph.Icon(icon);
    }

    return GetTextCap(GetKeyLabel(key));
  }

  // Gets a blank key cap carrying arbitrary text. Also used as the fallback for
  // gamepad bindings that have no icon of their own.
  public static InputGlyph? GetTextCap(string label) {
    var cap = GD.Load<Texture2D>(KeyboardIconPath + "btn.png");
    return cap == null ? null : InputGlyph.Keycap(cap, label);
  }

  // Gets the icon texture for the whole arrow key cluster (used by hints that
  // stand for "any direction", like menu navigation).
  public static Texture2D? GetArrowKeysIcon() => GD.Load<Texture2D>(KeyboardIconPath + "arrows.png");

  // Gets the icon texture for a single arrow key, cut out of the cluster sheet.
  public static Texture2D? GetArrowIcon(Key key) {
    if (!ArrowRegions.TryGetValue(key, out var region)) {
      return null;
    }

    if (_arrowIcons.TryGetValue(key, out var cached)) {
      return cached;
    }

    var atlas = GetArrowKeysIcon();
    if (atlas == null) {
      return null;
    }

    var icon = new AtlasTexture { Atlas = atlas, Region = region };
    _arrowIcons[key] = icon;
    return icon;
  }

  private static string GetKeyIconPath(Key key) => key switch {
    Key.Enter => KeyboardIconPath + "enter.png",
    Key.KpEnter => KeyboardIconPath + "enter.png",
    Key.Shift => KeyboardIconPath + "shift.png",
    _ => string.Empty
  };

  // Converts a key to the short, upper-case text printed on its cap. Named keys
  // whose Godot name is too long for a cap get the abbreviation a keyboard uses.
  private static string GetKeyLabel(Key key) => key switch {
    Key.Escape => "ESC",
    Key.Backspace => "BKSP",
    Key.Capslock => "CAPS",
    Key.Numlock => "NUM",
    Key.Scrolllock => "SCRL",
    Key.Delete => "DEL",
    Key.Insert => "INS",
    Key.Pageup => "PGUP",
    Key.Pagedown => "PGDN",
    Key.Print => "PRNT",
    Key.KpAdd => "+",
    Key.KpSubtract => "-",
    Key.KpMultiply => "*",
    Key.KpDivide => "/",
    Key.KpPeriod => ".",
    _ => PrintedKeyName(key)
  };

  // Character keys print their character (";" rather than "SEMICOLON"), and the
  // remaining named keys print their Godot name: "Kp 1" -> "1", "Space" ->
  // "SPACE". The cap stretches to whatever comes out, so nothing is truncated.
  private static string PrintedKeyName(Key key) {
    var keycode = (int)key;
    if (keycode is >= FirstPrintableKeycode and <= LastPrintableKeycode) {
      return ((char)keycode).ToString().ToUpperInvariant();
    }

    var name = OS.GetKeycodeString(key);
    if (name.StartsWith("Kp ")) {
      name = name[3..];
    }

    return name.ToUpperInvariant();
  }
}
