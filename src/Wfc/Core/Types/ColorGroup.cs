using Wfc.Skin;

namespace Wfc.Core.Types
{
  public class ColorGroup
  {
    public string Value { get; }
    public SkinColor SkinColor { get; }

    private ColorGroup(string value, SkinColor skinColor)
    {
      Value = value;
      SkinColor = skinColor;
    }

    public override string ToString()
    {
      return Value;
    }

    public static readonly ColorGroup Blue = new("Blue", SkinColor.TopFace);
    public static readonly ColorGroup Red = new("Red", SkinColor.LeftFace);
    public static readonly ColorGroup Purple = new("Purple", SkinColor.BottomFace);
    public static readonly ColorGroup Yellow = new("Yellow", SkinColor.RightFace);

    public static ColorGroup FromString(string value)
    {
      return value switch
      {
        "Blue" => Blue,
        "Red" => Red,
        "Purple" => Purple,
        "Yellow" => Yellow,
        _ => throw new System.Exception("Invalid color group")
      };
    }
  }

}