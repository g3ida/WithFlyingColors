namespace Wfc.Utils.Fonts;

using Godot;

public static class FontUtils {
  // Cap height as a fraction of the font size, measured on the menu font
  // (HeavyEquipment): capitals rise 18px at size 26. Godot exposes ascent and
  // descent but not cap height, and every label in the UI is set in capitals.
  private const float CapHeightRatio = 0.69f;

  // How far down to nudge a vertically centred, all-capitals label so its
  // capitals sit on the centre line. Centring aligns the line box, which
  // reserves descender room capitals never reach into, so the text ends up
  // looking a couple of pixels high.
  public static float OpticalCenterOffset(Font font, int fontSize) {
    var capHeight = CapHeightRatio * fontSize;
    return (font.GetHeight(fontSize) * 0.5f) - font.GetAscent(fontSize) + (capHeight * 0.5f);
  }
}
