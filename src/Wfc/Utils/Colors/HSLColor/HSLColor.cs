namespace Wfc.Utils.Colors;

using System;
using Godot;

public partial class HSLColor : RefCounted {
  public float H { get; set; }
  public float S { get; set; }
  public float L { get; set; }

  public HSLColor(float h, float s, float l) {
    H = h;
    S = s;
    L = l;
  }

  public HSLColor MakeDarker(float lShiftPercentage) {
    L = Mathf.Clamp(L + L * lShiftPercentage, 0, 255.0f);
    return this;
  }

  public Color ToRgb() {
    float d = S * (1 - Mathf.Abs(2 * L - 1));
    float m = L - 0.5f * d;
    float x = d * (1 - Mathf.Abs((H / 60.0f) % 2 - 1));
    Color col = new Color();

    if (H >= 0 && H < 60) {
      col.R = d + m;
      col.G = x + m;
      col.B = m;
    }
    else if (H >= 60 && H < 120) {
      col.R = x + m;
      col.G = d + m;
      col.B = m;
    }
    else if (H >= 120 && H < 180) {
      col.R = m;
      col.G = d + m;
      col.B = x + m;
    }
    else if (H >= 180 && H < 240) {
      col.R = m;
      col.G = x + m;
      col.B = d + m;
    }
    else if (H >= 240 && H < 300) {
      col.R = x + m;
      col.G = m;
      col.B = d + m;
    }
    else // H >= 300 && H <= 360
    {
      col.R = d + m;
      col.G = m;
      col.B = x + m;
    }

    return col;
  }
}
