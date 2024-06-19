using Godot;
using System;

[Tool]
public partial class TitleLabel : Label
{
    [Export]
    public string content { get; set; } = "";

    [Export]
    public Color underline_color { get; set; }

    [Export]
    public Color underline_shadow_color { get; set; }

    public void UpdatePositionX(float value)
    {
        Position = new Vector2(value, Position.Y);
    }

    public override void _Ready()
    {
        Text = content;
        GetNode<Label>("Shadow").Text = content;
        SetProcess(false);

        var scale = GetMinimumSize().X / GetNode<Control>("Underline").Size.X;
        GetNode<Control>("Underline").Scale = new Vector2(scale, GetNode<Control>("Underline").Scale.Y);
        GetNode<Control>("Underline").Modulate = underline_color;

        var shadowScale = GetMinimumSize().X / GetNode<Control>("UnderlineShadow").Size.X;
        GetNode<Control>("UnderlineShadow").Scale = new Vector2(shadowScale, GetNode<Control>("UnderlineShadow").Scale.Y);
        GetNode<Control>("UnderlineShadow").Modulate = underline_shadow_color;
    }
}
