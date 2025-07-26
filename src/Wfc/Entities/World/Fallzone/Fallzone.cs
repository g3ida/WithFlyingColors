namespace Wfc.Entities.World;

using Godot;
using Wfc.Core.Event;
using Wfc.Entities.World;

public partial class Fallzone : Area2D {
  private static void _onFallZoneAreaAreaEntered(Area2D area) {
    EventHandler.Instance.EmitPlayerDying(area.GlobalPosition, EntityType.FallZone);
  }
}
