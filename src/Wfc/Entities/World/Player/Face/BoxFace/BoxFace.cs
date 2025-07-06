namespace Wfc.Entities.World.Player;

using System;
using Godot;
using Wfc.Core.Event;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class BoxFace : BaseFace {

  public override void _Ready() {
    base._Ready();
    EdgeLength = ((CollisionShapeNode.Shape as RectangleShape2D)?.Size.X) ?? 0f;
  }

  public override void ScaleBy(float factor) {
    float scaleFactor = factor;
    Scale = new Vector2(scaleFactor, scaleFactor);

    Position = new Vector2(
        positionX + Extents.Y * (scaleFactor - 1.0f) * Math.Sign(Position.Y) * Mathf.Sin(Rotation),
        positionY - Extents.Y * (scaleFactor - 1.0f) * Math.Sign(Position.Y) * Mathf.Cos(Rotation)
    );
  }

  public void _on_bottomFace_area_entered(Area2D area) {
    var groups = GetGroups();
    //GD.Assert(groups.Count == 1);

    if (area.IsInGroup("fallzone")) {
      EventHandler.Instance.EmitPlayerDying(GlobalPosition, Constants.EntityType.FALL_ZONE);
      return;
    }

    if (area is Gem gem) {
      if (!gem.IsInGroup(groups[0])) {
        EventHandler.Instance.EmitPlayerDying(area, GlobalPosition, Constants.EntityType.PLATFORM);
      }
      else {
        gem._on_Gem_area_entered(this);
      }
    }
    else if (!area.IsInGroup(groups[0])) {
      EventHandler.Instance.EmitPlayerDying(area, GlobalPosition, Constants.EntityType.PLATFORM);
    }
    else if (!Global.Instance().Player.IsStanding()) {
      EventHandler.Instance.EmitPlayerLanded(area, GlobalPosition);
    }
  }
}
