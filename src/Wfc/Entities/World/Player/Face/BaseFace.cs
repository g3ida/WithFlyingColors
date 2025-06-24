namespace Wfc.Entities.World.Player;

using System;
using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

public partial class BaseFace : Area2D {
  protected CollisionShape2D CollisionShapeNode { get; private set; } = null!;
  public float EdgeLength { get; protected set; }

  protected Vector2 Extents;

  protected float positionX { get; private set; }
  protected float positionY { get; private set; }

  public override void _Ready() {
    CollisionShapeNode = GetNode<CollisionShape2D>("CollisionShape2D");
    Extents = (CollisionShapeNode.Shape as RectangleShape2D)?.Size ?? Vector2.Zero;
    positionX = Position.X;
    positionY = Position.Y;
  }

  public bool CheckGroup(Area2D area, string[] groups) {
    // FIXME: remove redundant code
    if (area is Gem gem) {
      foreach (string group in groups) {
        if (gem.IsInGroup(group)) {
          return true;
        }
      }
    }
    else {
      foreach (string group in groups) {
        if (area.IsInGroup(group)) {
          return true;
        }
      }
    }
    return false;
  }

  public virtual void ScaleBy(float factor) {
    float scaleFactor = factor;
    Scale = new Vector2(scaleFactor, scaleFactor);
    Position = new Vector2(
        positionX - Extents.X * (scaleFactor - 1.0f) * Math.Sign(Position.X) * 0.5f,
        positionY - Extents.Y * (scaleFactor - 1.0f) * Math.Sign(Position.Y) * 0.5f
    );
  }
}
