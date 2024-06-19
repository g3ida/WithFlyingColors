using Godot;

public partial class PowerUpSprite : Sprite2D
{
    public override void _Ready()
    {
        int colorIndex = ColorUtils.GetGroupColorIndex(GetParent().Get("color_group").ToString());
        Color color = ColorUtils.GetBasicColor(colorIndex);
        Modulate = ColorUtils.DarkenRGB(color, 0.4f);
    }
}
