namespace Wfc.Entities.World.Player;

using System;
using Godot;
using Wfc.Core.Event;
using Wfc.Entities.World.Gems;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class BoxFace : BaseFace {

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
    EdgeLength = ((CollisionShapeNode.Shape as RectangleShape2D)?.Size.X) ?? 0f;
  }

  public override void ScaleBy(float factor) {
    float scaleFactor = factor;
    Scale = new Vector2(scaleFactor, scaleFactor);

    Position = new Vector2(
        PositionX + Extents.Y * (scaleFactor - 1.0f) * Math.Sign(Position.Y) * Mathf.Sin(Rotation),
        PositionY - Extents.Y * (scaleFactor - 1.0f) * Math.Sign(Position.Y) * Mathf.Cos(Rotation)
    );
  }

  public void _onAreaEntered(Area2D area) {
    if (Global.Instance().Player.IsDying()) {
      return;
    }
    var groups = GetGroups();

    if (area.IsInGroup("fallzone")) {
      EventHandler.Instance.EmitPlayerDying(GlobalPosition, EntityType.FallZone);
      return;
    }
    else if (!area.IsInGroup(groups[0])) {
      EventHandler.Instance.EmitPlayerDying(area, GlobalPosition, EntityType.Platform);
    }
    else if (!Global.Instance().Player.IsStanding()) {
      EventHandler.Instance.EmitPlayerLanded(area, GlobalPosition);
    }
  }
}
