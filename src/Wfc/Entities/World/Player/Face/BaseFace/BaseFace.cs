namespace Wfc.Entities.World.Player;

using System;
using Godot;
using Wfc.Entities.World.Gems;
using Wfc.Utils;
using Wfc.Utils.Attributes;

public partial class BaseFace : Area2D {
  protected CollisionShape2D CollisionShapeNode { get; private set; } = null!;
  public float EdgeLength { get; protected set; }

  protected Vector2 Extents;

  protected float PositionX { get; private set; }
  protected float PositionY { get; private set; }

  public override void _Ready() {
    CollisionShapeNode = GetNode<CollisionShape2D>("CollisionShape2D");
    Extents = (CollisionShapeNode.Shape as RectangleShape2D)?.Size ?? Vector2.Zero;
    PositionX = Position.X;
    PositionY = Position.Y;
  }

  public virtual void ScaleBy(float factor) {
    float scaleFactor = factor;
    Scale = new Vector2(scaleFactor, scaleFactor);
    Position = new Vector2(
        PositionX - Extents.X * (scaleFactor - 1.0f) * Math.Sign(Position.X) * 0.5f,
        PositionY - Extents.Y * (scaleFactor - 1.0f) * Math.Sign(Position.Y) * 0.5f
    );
  }
}
