using Godot;
using System;

public class BlockSprite : Node2D
{
    [Export]
    public string color_group
    {
        get => colorGroup;
        set => SetGroup(value);
    }
    private string colorGroup = "blue";

    public override void _Ready() { }

    public void SetGroup(string group)
    {
        colorGroup = group;
        int colorIndex = ColorUtils.GetGroupColorIndex(colorGroup);
        Color baseColor = ColorUtils.GetBasicColor(colorIndex);
        Color lightColor = ColorUtils.GetLightColor(colorIndex);
        Color darkColor = ColorUtils.GetDarkColor(colorIndex);
        
        // Apply colors to the sprite layers
        GetNode<Sprite>("Layer1").Modulate = lightColor;
        GetNode<Sprite>("Layer2").Modulate = darkColor;
        GetNode<Sprite>("TopLayer").Modulate = baseColor;
    }

    public string GetGroup()
    {
        return colorGroup;
    }
}
