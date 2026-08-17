namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Core.Event;
using Wfc.Entities.World.Gems;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class BoxCorner : BaseFace {
  private Vector2 _outerCorner;

  // How far this corner's outer corner stands from the cube's center along each axis. The seam
  // grows about it, so making the corners more forgiving never changes the cube's silhouette.
  public float OuterReach => Mathf.Abs(_outerCorner.X);

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
    _outerCorner = Position + (Position.Sign() * EdgeLength * 0.5f);
  }

  public void SetSeamSide(float side) {
    if (ShapeRect is not null) {
      ShapeRect.Size = new Vector2(side, side);
    }
    Position = _outerCorner - (_outerCorner.Sign() * side * 0.5f);
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

    if (!AcceptsColorOf(area)) {
      GameEvents.Instance.OnPlayerDying(area, GlobalPosition, EntityType.Platform);
    }
    else if (area is Gem gem) {
      // do nothing
    }
    else if (!player.IsStanding()) {
      GameEvents.Instance.OnPlayerLandedOn(area, GlobalPosition);
    }
  }
}
