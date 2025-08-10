namespace Wfc.Core.Types;

using Godot;
using Wfc.Skin;
using Wfc.Utils.Colors;

public partial class ColorGroup : GodotObject {
  public string Value { get; }
  public SkinColor SkinColor { get; }

  private ColorGroup(string value, SkinColor skinColor) {
    Value = value;
    SkinColor = skinColor;
  }

  public override string ToString() => Value;

  public static readonly ColorGroup Blue = new(ColorUtils.BLUE, SkinColor.TopFace);
  public static readonly ColorGroup Pink = new(ColorUtils.PINK, SkinColor.LeftFace);
  public static readonly ColorGroup Purple = new(ColorUtils.PURPLE, SkinColor.BottomFace);
  public static readonly ColorGroup Yellow = new(ColorUtils.YELLOW, SkinColor.RightFace);

  public static ColorGroup FromString(string value) => value switch {
    ColorUtils.BLUE => Blue,
    ColorUtils.PINK => Pink,
    ColorUtils.PURPLE => Purple,
    ColorUtils.YELLOW => Yellow,
    _ => throw new System.ArgumentException("Invalid color group")
  };
}
