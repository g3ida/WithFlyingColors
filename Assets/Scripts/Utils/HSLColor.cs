using Godot;
using System;

public class HSLColor : Reference
{
    public float H { get; set; }
    public float S { get; set; }
    public float L { get; set; }

    public HSLColor(float h, float s, float l)
    {
        H = h;
        S = s;
        L = l;
    }

    public HSLColor MakeDarker(float lShiftPercentage)
    {
        L = Mathf.Clamp(L + L * lShiftPercentage, 0, 255.0f);
        return this;
    }

    public Color ToRgb()
    {
        float d = S * (1 - Mathf.Abs(2 * L - 1));
        float m = L - 0.5f * d;
        float x = d * (1 - Mathf.Abs((H / 60.0f) % 2 - 1));
        Color col = new Color();

        if (H >= 0 && H < 60)
        {
            col.r = d + m;
            col.g = x + m;
            col.b = m;
        }
        else if (H >= 60 && H < 120)
        {
            col.r = x + m;
            col.g = d + m;
            col.b = m;
        }
        else if (H >= 120 && H < 180)
        {
            col.r = m;
            col.g = d + m;
            col.b = x + m;
        }
        else if (H >= 180 && H < 240)
        {
            col.r = m;
            col.g = x + m;
            col.b = d + m;
        }
        else if (H >= 240 && H < 300)
        {
            col.r = x + m;
            col.g = m;
            col.b = d + m;
        }
        else // H >= 300 && H <= 360
        {
            col.r = d + m;
            col.g = m;
            col.b = x + m;
        }

        return col;
    }
}
