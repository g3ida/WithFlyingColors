using Godot;
using System;

public partial class BoxFace : BaseFace {
  public float edgeLength;

  public override void _Ready() {
    base._Ready();
    collisionShapeNode = GetNode<CollisionShape2D>("CollisionShape2D");
    edgeLength = (collisionShapeNode.Shape as RectangleShape2D).Size.X;
  }

  public override void ScaleBy(float factor) {
    float scaleFactor = factor;
    Scale = new Vector2(scaleFactor, scaleFactor);

    Position = new Vector2(
        positionX + extents.Y * (scaleFactor - 1.0f) * Math.Sign(Position.Y) * Mathf.Sin(Rotation),
        positionY - extents.Y * (scaleFactor - 1.0f) * Math.Sign(Position.Y) * Mathf.Cos(Rotation)
    );
  }

  public void _on_bottomFace_area_entered(Area2D area) {
    var groups = GetGroups();
    //GD.Assert(groups.Count == 1);

    if (area.IsInGroup("fallzone")) {
      Event.Instance.EmitPlayerDying(null, GlobalPosition, Constants.EntityType.FALL_ZONE);
      return;
    }

    if (area is Gem gem) {
      if (!gem.IsInGroup((string)groups[0])) {
        Event.Instance.EmitPlayerDying(area, GlobalPosition, Constants.EntityType.PLATFORM);
      }
      else {
        gem._on_Gem_area_entered(this);
      }
    }
    else if (!area.IsInGroup((string)groups[0])) {
      Event.Instance.EmitPlayerDying(area, GlobalPosition, Constants.EntityType.PLATFORM);
    }
    else if (!Global.Instance().Player.IsStanding()) {
      Event.Instance.EmitPlayerLanded(area, GlobalPosition);
    }
  }
}
