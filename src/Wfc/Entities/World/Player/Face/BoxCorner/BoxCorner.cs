namespace Wfc.Entities.World.Player;

using System;
using Godot;
using Wfc.Core.Event;
using Wfc.Entities.World.Gems;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class BoxCorner : BaseFace {

  public override void _EnterTree() {
    base._EnterTree();
    AreaEntered += _onAreaEntered;
  }

  public override void _ExitTree() {
    base._ExitTree();
    AreaEntered -= _onAreaEntered;
  }

  public override void _Ready() {
    base._Ready();
    RectangleShape2D? collisionShape = CollisionShapeNode.Shape as RectangleShape2D;
    EdgeLength = collisionShape?.Size.X ?? 0;
  }

  public void _onAreaEntered(Area2D area) {
    var player = GetParent<Player>();
    if (player == null) {
      // Fixme: Log error here. this should not happen anyways.
      return;
    }
    if (player.IsDying()) {
      return;
    }
    if (area.IsInGroup("fallzone")) {
      EventHandler.Instance.EmitPlayerDying(GlobalPosition, EntityType.FallZone);
      return;
    }

    if (!AcceptsColorOf(area)) {
      EventHandler.Instance.EmitPlayerDying(area, GlobalPosition, EntityType.Platform);
    }
    else if (area is Gem gem) {
      // do nothing
    }
    else if (!player.IsStanding()) {
      EventHandler.Instance.EmitPlayerLanded(area, GlobalPosition);
    }
  }
}
