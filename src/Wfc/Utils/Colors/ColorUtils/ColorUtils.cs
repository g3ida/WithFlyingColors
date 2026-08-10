namespace Wfc.Utils.Colors;

using System;
using System.Collections.Generic;
using Godot;

public partial class ColorUtils : Node {
  public const string BLUE = "blue";
  public const string PINK = "pink";
  public const string YELLOW = "yellow";
  public const string PURPLE = "purple";
  // For iterating over the four colors and for validating one. Order carries no meaning here -
  // see FromTileSourceId for the one place a color is identified by its index.
  public static readonly string[] COLOR_GROUPS = { BLUE, PINK, YELLOW, PURPLE };

  // The tile source ids saved into the brick-breaker tilemaps. These integers are level data:
  // every arena `.tscn` in the project names its bricks' colors this way, so changing the mapping
  // recolors every arena ever authored - and since a face of the wrong color is fatal, it rewrites
  // which bricks kill the player.
  //
  // The mapping used to be nothing more than the position of a name inside COLOR_GROUPS, an array
  // with no [Export], no comment and two other declaration orders elsewhere in the codebase to
  // disagree with (ColorGroup declares Blue/Pink/Purple/Yellow, GameSkin slots blue/pink/purple/
  // yellow) - so reordering what looks like a cosmetic literal silently rewrote the levels.
  public static string? FromTileSourceId(int tileSourceId) => tileSourceId switch {
    0 => BLUE,
    1 => PINK,
    2 => YELLOW,
    3 => PURPLE,
    _ => null,
  };

  public const int TILE_SOURCE_ID_COUNT = 4;

  // The color a node is tagged with, picked out of whatever else it has been grouped into.
  // Groups are how every colored thing in the game carries its color, so anything that has to
  // draw in a partner's color starts here.
  public static string? ColorGroupOf(Node node) {
    foreach (var group in node.GetGroups()) {
      var name = group.ToString();
      if (Array.IndexOf(COLOR_GROUPS, name) >= 0) {
        return name;
      }
    }
    return null;
  }

  public static HSLColor RgbToHsl(Color color) {
    float R = color.R * 255.0f;
    float G = color.G * 255.0f;
    float B = color.B * 255.0f;
    float M = Math.Max(Math.Max(R, G), B);
    float m = Math.Min(Math.Min(R, G), B);
    float d = (M - m) / 255.0f;
    float L = (0.5f * (M + m)) / 255.0f;
    float S = L <= 0.0f ? 0.0f : d / (1 - Math.Abs(2 * L - 1) + 0.001f);
    float t = Mathf.Acos((R - 0.5f * G - 0.5f * B) / Mathf.Sqrt((R * R + G * G + B * B - R * G - R * B - G * B) + 0.001f)) * MathUtils.RAD_TO_DEGREES;
    float H = B > G ? 360.0f - t : t;
    return new HSLColor(H, S, L);
  }

  public static Color DarkenRGB(Color color, float lShiftPercentage) {
    return RgbToHsl(color).MakeDarker(-lShiftPercentage).ToRgb();
  }
}
