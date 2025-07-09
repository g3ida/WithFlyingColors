namespace Wfc.Entities.World.Player;

using System;
using Godot;
using Wfc.Core.Event;
using Wfc.Entities.World.Gems;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class BoxCorner : BaseFace {

  public override void _Ready() {
    base._Ready();
    RectangleShape2D? collisionShape = CollisionShapeNode.Shape as RectangleShape2D;
    EdgeLength = collisionShape?.Size.X ?? 0;
  }

  public void _on_area_entered(Area2D area) {
    if (area.IsInGroup("fallzone")) {
      EventHandler.Instance.EmitPlayerDying(GlobalPosition, Constants.EntityType.FALL_ZONE);
      return;
    }

    var groups = GetGroups();
    if (!_checkGroup(area, groups)) {
      EventHandler.Instance.EmitPlayerDying(area, GlobalPosition, Constants.EntityType.PLATFORM);
    }
    else
    if (area is Gem gem) {
      gem._on_Gem_area_entered(this);
    }
    else if (!Global.Instance().Player.IsStanding()) {
      EventHandler.Instance.EmitPlayerLanded(area, GlobalPosition);
    }
  }

  private static bool _checkGroup(Area2D area, Godot.Collections.Array<StringName> groups) {
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
