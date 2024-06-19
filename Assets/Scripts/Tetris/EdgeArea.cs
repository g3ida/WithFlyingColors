using Godot;
using System;

public partial class EdgeArea : Area2D {
    public CollisionShape2D collisionShapeNode;

    public float Width { get; private set; }
    public float Height { get; private set; }

    public override void _Ready() {
        collisionShapeNode = new CollisionShape2D();
        var rectangleShape = new RectangleShape2D();
        Width = rectangleShape.Size.X;
        Height = rectangleShape.Size.Y;
        collisionShapeNode.Shape = rectangleShape;
        AddChild(collisionShapeNode);
    }
}
