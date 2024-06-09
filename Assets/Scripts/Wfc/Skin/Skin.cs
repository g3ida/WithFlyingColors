using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Wfc.Skin;

namespace Wfc.Skin
{

  public partial record Skin
  {
    public string Name { get; }
    private Dictionary<SkinColorIntensity, Dictionary<SkinColor, Godot.Color>> _colors { get; }

    public Skin(string name, string[][] colors)
    {
      Name = name;
      Debug.Assert(colors != null, "Colors should not be null");
      Debug.Assert(colors.Length == 6, "Colors should have 6 intensities");
      Debug.Assert(colors.All(x => x.Length == 4), "Each intensity should have exactly 4 colors");

      var SkinColorIntensities = new SkinColorIntensity[] {
            SkinColorIntensity.VeryDark,
            SkinColorIntensity.Dark,
            SkinColorIntensity.Basic,
            SkinColorIntensity.Light,
            SkinColorIntensity.VeryLight,
            SkinColorIntensity.Background
        };
      _colors = colors.Select((el, index) => new KeyValuePair<SkinColorIntensity, Dictionary<SkinColor, Godot.Color>>(
          SkinColorIntensities[index],
          new Dictionary<SkinColor, Godot.Color>
          {
                { SkinColor.BottomFace, new Godot.Color(el[2]) },
                { SkinColor.RightFace, new Godot.Color(el[3]) },
                { SkinColor.TopFace,new Godot.Color( el[0]) },
                { SkinColor.LeftFace, new Godot.Color(el[1]) }
          }
      )).ToDictionary(x => x.Key, x => x.Value);
    }

    public Dictionary<SkinColor, Godot.Color> GetColors(SkinColorIntensity intensity)
    {
      _colors.TryGetValue(intensity, out var colors);
      return colors;
    }

    public Godot.Color GetColor(SkinColor skinColor, SkinColorIntensity intensity)
    {
      _colors.TryGetValue(intensity, out var colors);
      return colors[skinColor];
    }
  }

}