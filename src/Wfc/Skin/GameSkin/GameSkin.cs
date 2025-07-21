namespace Wfc.Skin;

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Wfc.Core.Exceptions;
using Wfc.Utils.Colors;

public partial record GameSkin {
  public string Name { get; }
  private Dictionary<SkinColorIntensity, Dictionary<SkinColor, Godot.Color>> Colors { get; }

  public GameSkin(string name, string[][] colors) {
    Name = name;
    Debug.Assert(colors != null, "Colors should not be null");
    Debug.Assert(colors.Length == 6, "Colors should have 6 intensities");
    Debug.Assert(colors.All(x => x.Length == 4), "Each intensity should have exactly 4 colors");

    var skinColorIntensities = new SkinColorIntensity[] {
          SkinColorIntensity.VeryDark,
          SkinColorIntensity.Dark,
          SkinColorIntensity.Basic,
          SkinColorIntensity.Light,
          SkinColorIntensity.VeryLight,
          SkinColorIntensity.Background
      };
    Colors = colors.Select((el, index) => new KeyValuePair<SkinColorIntensity, Dictionary<SkinColor, Godot.Color>>(
        skinColorIntensities[index],
        new Dictionary<SkinColor, Godot.Color>
        {
              { SkinColor.BottomFace, new Godot.Color(el[2]) },
              { SkinColor.RightFace, new Godot.Color(el[3]) },
              { SkinColor.TopFace,new Godot.Color( el[0]) },
              { SkinColor.LeftFace, new Godot.Color(el[1]) }
        }
    )).ToDictionary(x => x.Key, x => x.Value);
  }

  public Dictionary<SkinColor, Godot.Color> GetColors(SkinColorIntensity intensity) {
    Colors.TryGetValue(intensity, out var colors);
    if (colors == null) {
      throw new KeyNotFoundException("No colors found for intensity: " + intensity);
    }
    return colors;
  }

  public Godot.Color GetColor(SkinColor skinColor, SkinColorIntensity intensity) {
    Colors.TryGetValue(intensity, out var colors);
    if (colors == null) {
      throw new KeyNotFoundException("No colors found for intensity: " + intensity);
    }
    return colors[skinColor];
  }

  public static SkinColor ColorGroupToSkinColor(string colorGroup) {
    switch (colorGroup) {
      case ColorUtils.BLUE:
        return SkinColor.TopFace;
      case ColorUtils.YELLOW:
        return SkinColor.RightFace;
      case ColorUtils.PURPLE:
        return SkinColor.BottomFace;
      case ColorUtils.PINK:
        return SkinColor.LeftFace;
      default:
        throw new GameExceptions.InvalidArgumentException("Invalid color group: " + colorGroup);
    }
  }
}
