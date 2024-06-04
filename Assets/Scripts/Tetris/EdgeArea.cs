using Godot;
using System;

public partial class EdgeArea : Area2D
{
    public CollisionShape2D collisionShapeNode;

    public float Width { get; private set; }
    public float Height { get; private set; }

    public override void _Ready()
    {
        collisionShapeNode = new CollisionShape2D();
        var rectagleShape = new RectangleShape2D();
        Width = rectagleShape.Size.X;
        Height = rectagleShape.Size.Y;
        collisionShapeNode.Shape = rectagleShape;
        AddChild(collisionShapeNode);
    }
}
