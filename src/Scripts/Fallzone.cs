using Godot;
using Wfc.Core.Event;

public partial class Fallzone : Area2D {
  private void _on_FallZoneArea_area_entered(Area2D area) {
    EventHandler.Instance.EmitPlayerDying(area.GlobalPosition, Constants.EntityType.FALL_ZONE);
  }
}
