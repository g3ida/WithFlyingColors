namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Core.Event;
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
      GameEvents.Instance.OnPlayerDying(GlobalPosition, EntityType.FallZone);
      return;
    }
    else if (!AcceptsColorOf(area)) {
      GameEvents.Instance.OnPlayerDying(area, GlobalPosition, EntityType.Platform);
    }
    else if (!player.IsStanding()) {
      GameEvents.Instance.OnPlayerLandedOn(area, GlobalPosition);
    }
  }
}
