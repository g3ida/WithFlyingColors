using Godot;
using System;

public partial class BoxCorner : BaseFace {
  public float edgeLength;

  public override void _Ready() {
    base._Ready();
    collisionShapeNode = GetNode<CollisionShape2D>("CollisionShape2D");
    edgeLength = (collisionShapeNode.Shape as RectangleShape2D).Size.X;
  }

  public void _on_area_entered(Area2D area) {
    if (area.IsInGroup("fallzone")) {
      Event.Instance.EmitPlayerDying(null, GlobalPosition, Constants.EntityType.FALL_ZONE);
      return;
    }

    var groups = GetGroups();
    if (!CheckGroup(area, groups)) {
      Event.Instance.EmitPlayerDying(area, GlobalPosition, Constants.EntityType.PLATFORM);
    }
    else
    if (area is Gem gem) {
      gem._on_Gem_area_entered(this);
    }
    else if (!Global.Instance().Player.IsStanding()) {
      Event.Instance.EmitPlayerLanded(area, GlobalPosition);
    }
  }

  private bool CheckGroup(Area2D area, Godot.Collections.Array<StringName> groups) {
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
}
