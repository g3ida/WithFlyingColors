using Godot;
using System;

public partial class BouncingBallSprite : Sprite2D
{
    public void SetColor(string colorGroup)
    {
        int colorIndex = ColorUtils.GetGroupColorIndex(colorGroup);
        Color color = ColorUtils.GetBasicColor(colorIndex);
        this.Modulate = color;
    }

    public override void _Ready()
    {
        // Optional: Add any initialization code here.
    }
}
