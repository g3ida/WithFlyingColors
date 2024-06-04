using Godot;
using System;
using System.Collections.Generic;

public partial class ColorUtils : Node
{
    public static readonly string[] COLORS = { "blue", "pink", "yellow", "purple" };
    public static readonly Color DARK_GREY = new Color(0.4f, 0.4f, 0.4f, 1f);

    public static int GetGroupColorIndex(string groupName)
    {
        switch (groupName)
        {
            case "blue":
                return 0;
            case "pink":
                return 1;
            case "yellow":
                return 3;
            case "purple":
                return 2;
            default:
                GD.PushError("Unknown group: " + groupName);
                return 0;
        }
    }

    public static Color GetBasicColor(int colorIndex)
    {
        return GetSkinBasicColor(GetCurrentSkin(), colorIndex);
    }

    public static Color GetSkinBasicColor(Dictionary<string, string[]> skin, int colorIndex)
    {
        return GetSkinColor(skin, colorIndex + "-basic");
    }

    public static Color GetLightColor(int colorIndex)
    {
        return GetColor(colorIndex + "-light");
    }

    public static Color GetLight2Color(int colorIndex)
    {
        return GetColor(colorIndex + "-light2");
    }

    public static Color GetDarkColor(int colorIndex)
    {
        return GetColor(colorIndex + "-dark");
    }

    public static Color GetColor(string colorName)
    {
        return GetSkinColor(GetCurrentSkin(), colorName);
    }

    private static Dictionary<string, string[]> GetCurrentSkin()
    {
        if (Engine.IsEditorHint())
        {
            return SkinLoader.DEFAULT_SKIN;
        }
        else
        {
            return Global.Instance().GetSelectedSkin();
        }
    }

    public static Color GetSkinColor(Dictionary<string, string[]> skin, string colorName)
    {
        // ie: 0-basic, 1-dark2, 2-light, 3-background
        var colorComponent = colorName.Split("-");
        if (colorComponent.Length != 2)
        {
            GD.PushError("Wrong requested color name: " + colorName);
            return new Color(0, 0, 0, 0);
        }
        int index = int.Parse(colorComponent[0]);
        string intensity = colorComponent[1];
        if (!Array.Exists(SkinLoader.KEYS, element => element == intensity))
        {
            GD.PushError("Wrong color intensity: " + intensity);
            intensity = "basic";
        }
        return new Color(skin[intensity][index]);
    }

    public static HSLColor RgbToHsl(Color color)
    {
        float R = color.R * 255.0f;
        float G = color.G * 255.0f;
        float B = color.B * 255.0f;
        float M = Math.Max(Math.Max(R, G), B);
        float m = Math.Min(Math.Min(R, G), B);
        float d = (M - m) / 255.0f;
        float L = (0.5f * (M + m)) / 255.0f;
        float S = L <= 0.0f ? 0.0f : d / (1 - Math.Abs(2 * L - 1) + 0.001f);
        float t = Mathf.Acos((R - 0.5f * G - 0.5f * B) / Mathf.Sqrt((R * R + G * G + B * B - R * G - R * B - G * B) + 0.001f)) * Constants.RAD_TO_DEGREES;
        float H = B > G ? 360.0f - t : t;
        return new HSLColor(H, S, L);
    }

    public static Color DarkenRGB(Color color, float lShiftPercentage)
    {
        return RgbToHsl(color).MakeDarker(-lShiftPercentage).ToRgb();
    }
}
