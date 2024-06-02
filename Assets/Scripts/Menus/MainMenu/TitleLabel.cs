using Godot;
using System;

[Tool]
public class TitleLabel : Label
{
    [Export]
    public string content { get; set; } = "";

    [Export]
    public Color underline_color { get; set; }

    [Export]
    public Color underline_shadow_color { get; set; }

    public void UpdatePositionX(float value)
    {
        RectPosition = new Vector2(value, RectPosition.y);
    }

    public override void _Ready()
    {
        Text = content;
        GetNode<Label>("Shadow").Text = content;
        SetProcess(false);

        var scale = GetMinimumSize().x / GetNode<Control>("Underline").RectSize.x;
        GetNode<Control>("Underline").RectScale = new Vector2(scale, GetNode<Control>("Underline").RectScale.y);
        GetNode<Control>("Underline").Modulate = underline_color;

        var shadowScale = GetMinimumSize().x / GetNode<Control>("UnderlineShadow").RectSize.x;
        GetNode<Control>("UnderlineShadow").RectScale = new Vector2(shadowScale, GetNode<Control>("UnderlineShadow").RectScale.y);
        GetNode<Control>("UnderlineShadow").Modulate = underline_shadow_color;
    }
}
