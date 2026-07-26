namespace Wfc.Entities.Ui.InputHint;

using Godot;
using Wfc.Utils;
using Wfc.Utils.Fonts;

// Draws a single InputGlyph at a fixed row height: a plain sprite for gamepad
// buttons and for keys with dedicated art (shift, enter or arrow keys),
// or a blank key cap stretched to fit the text overlaid on it.
//
// The cap is laid out at the sprite's native pixel size and the whole
// NinePatchRect is scaled down afterwards, because a nine patch drawn straight
// at a smaller size keeps its patch margins at 1:1 and squashes its corners.
public partial class InputGlyphView : Control {
  // Native size of the key cap art (Assets/Sprites/controller/keyboard/btn.png).
  private const float CapNativeHeight = 55f;
  private const float CapNativeMinWidth = 54f;

  // Room the label needs on either side of its cap, in native pixels.
  private const float CapNativePaddingX = 34f;

  // The bottom rows of the cap art are its shadow lip rather than the face the
  // label sits on, so the label is centred on what's above them.
  private const float CapNativeLipHeight = 4f;

  // Height every glyph is drawn at, whatever its source art measures. 48 is the
  // native height of the gamepad art, so those icons stay pixel exact.
  [Export]
  public float GlyphHeight { get; set; } = 48f;

  // Widest a glyph may get. Shoulder buttons and start/back are far wider than
  // they are tall (lb.png is 77x44), so sizing purely by height would let them
  // tower over the round face buttons.
  [Export]
  public float MaxGlyphWidth { get; set; } = 64f;

  private TextureRect _icon = default!;
  private NinePatchRect _cap = default!;
  private Label _keyLabel = default!;

  public override void _Ready() {
    base._Ready();
    _icon = GetNode<TextureRect>("Icon");
    _cap = GetNode<NinePatchRect>("Cap");
    _keyLabel = GetNode<Label>("Cap/KeyLabel");
  }

  // Draws the given glyph. Call this once the view is in the tree: measuring the
  // label needs the theme its ancestors provide.
  public void SetGlyph(InputGlyph glyph) {
    if (glyph.Label == null) {
      _showIcon(glyph.Texture);
    }
    else {
      _showCap(glyph.Texture, glyph.Label);
    }
  }

  private void _showIcon(Texture2D texture) {
    _cap.Visible = false;
    _icon.Visible = true;
    _icon.Texture = texture;

    // Fitted into the glyph box rather than stretched to its height, so wide art
    // keeps its proportions without dwarfing the square icons beside it.
    var width = texture.GetWidth();
    var height = texture.GetHeight();
    var scale = width <= 0 || height <= 0
        ? 1f
        : Mathf.Min(GlyphHeight / height, MaxGlyphWidth / width);

    var size = new Vector2(width * scale, height * scale);
    _icon.Size = size;
    CustomMinimumSize = size;
  }

  private void _showCap(Texture2D texture, string label) {
    _icon.Visible = false;
    _cap.Visible = true;
    _cap.Texture = texture;
    _keyLabel.Text = label;

    var scale = GlyphHeight / CapNativeHeight;
    var font = _keyLabel.GetThemeFont("font");
    var fontSize = _keyLabel.GetThemeFontSize("font_size");
    var textWidth = font.GetStringSize(label, HorizontalAlignment.Center, -1, fontSize).X;
    var capWidth = Mathf.Max(textWidth + CapNativePaddingX, CapNativeMinWidth);

    // Centre the capitals on the cap face: down by the font's optical offset,
    // and clear of the shadow lip along the bottom.
    var nudge = FontUtils.OpticalCenterOffset(font, fontSize);
    _keyLabel.OffsetTop = nudge;
    _keyLabel.OffsetBottom = nudge - CapNativeLipHeight;

    _cap.Size = new Vector2(capWidth, CapNativeHeight);
    _cap.Scale = new Vector2(scale, scale);
    CustomMinimumSize = new Vector2(capWidth * scale, GlyphHeight);
  }
}
