namespace Wfc.Utils.Colors;

using System;
using System.Collections.Generic;
using Godot;

public partial class ColorUtils : Node {
  public const string BLUE = "blue";
  public const string PINK = "pink";
  public const string YELLOW = "yellow";
  public const string PURPLE = "purple";
  public static readonly string[] COLOR_GROUPS = { BLUE, PINK, YELLOW, PURPLE };

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
