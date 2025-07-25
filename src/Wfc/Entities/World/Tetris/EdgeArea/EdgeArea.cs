namespace Wfc.Entities.Tetris;

using System;
using Godot;

public partial class EdgeArea : Area2D {
  public CollisionShape2D? CollisionShapeNode;

  public float Width { get; private set; }
  public float Height { get; private set; }

  public override void _Ready() {
    base._Ready();
    CollisionShapeNode = new CollisionShape2D();
    var rectangleShape = new RectangleShape2D();
    Width = rectangleShape.Size.X;
    Height = rectangleShape.Size.Y;
    CollisionShapeNode.Shape = rectangleShape;
    AddChild(CollisionShapeNode);
  }
}
