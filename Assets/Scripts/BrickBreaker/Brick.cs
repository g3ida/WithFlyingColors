using Godot;
using System;

[Tool]
public class Brick : Node2D
{
    [Signal]
    public delegate void brick_broken();

    [Export] public string color_group { get; set; } = "blue";

    private Area2D _areaNode;
    private Sprite _spriteNode;
    private CollisionShape2D _collisionShapeNode;

    public override void _Ready()
    {
        _areaNode = GetNode<Area2D>("Area2D");
        _spriteNode = GetNode<Sprite>("BrickSpr");
        _collisionShapeNode = GetNode<CollisionShape2D>("KinematicBody2D/CollisionShape2D");

        _areaNode.AddToGroup(color_group);
        SetColor();
    }

    private void SetColor()
    {
        int colorIndex = ColorUtils.GetGroupColorIndex(color_group);
        Color color = ColorUtils.GetBasicColor(colorIndex);
        _spriteNode.Modulate = color;
    }

    private void _on_Area2D_area_entered(Area2D area)
    {
        Vector2 extents = (_collisionShapeNode.Shape as RectangleShape2D).Extents;
        EmitSignal(nameof(brick_broken));
        Event.Instance().EmitBrickBroken(color_group, Position + (GetParent() as Node2D).Position + extents);
        QueueFree();
    }
}
