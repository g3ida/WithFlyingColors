namespace Wfc.Utils;

using Godot;

// One drawable input glyph: the sprite to show and, when that sprite is a blank
// key cap, the text to overlay on it. Gamepad glyphs and keys drawn as symbols
// on a real keyboard (enter, shift, the arrows) carry no label.
public readonly record struct InputGlyph(Texture2D Texture, string? Label = null) {
  public static InputGlyph Icon(Texture2D texture) => new(texture);

  public static InputGlyph Keycap(Texture2D texture, string label) => new(texture, label);
}
