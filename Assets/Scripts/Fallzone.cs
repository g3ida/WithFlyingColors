using Godot;
using System;

public partial class FallZone : Area2D {
  private void _on_FallZoneArea_area_entered(Area2D area) {
    Event.Instance.EmitPlayerDying(null, area.GlobalPosition, Constants.EntityType.FALL_ZONE);
  }
}
