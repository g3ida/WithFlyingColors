using Godot;
using Wfc.Core.Event;

[Tool]
public partial class LazerBeam : Node2D {
  private Line2D beamNode;
  private Line2D beamBgNode;
  private Marker2D muzzleNode;
  private CpuParticles2D particlesNode;
  private Sprite2D baseNode;

  [Export]
  public string color_group { get; set; }

  public override void _Ready() {
    beamNode = GetNode<Line2D>("Line2D");
    beamBgNode = GetNode<Line2D>("Line2DBackground");
    muzzleNode = GetNode<Marker2D>("Muzzle");
    particlesNode = GetNode<CpuParticles2D>("Particles");
    baseNode = GetNode<Sprite2D>("Base");

    int colorIndex = ColorUtils.GetGroupColorIndex(color_group);
    Color color = ColorUtils.GetBasicColor(colorIndex);
    Color darkColor = ColorUtils.GetDarkColor(colorIndex);
    beamNode.DefaultColor = color;
    beamBgNode.DefaultColor = color;
    beamBgNode.DefaultColor = new Color(beamBgNode.DefaultColor, 0.63f);
    particlesNode.Color = darkColor;
    baseNode.Modulate = color;
  }

  public PhysicsDirectSpaceState2D SpaceState => GetWorld2D().DirectSpaceState;

  public Godot.Collections.Dictionary CastBeam() {
    var physicsRayQueryParameters = PhysicsRayQueryParameters2D.Create(
        muzzleNode.GlobalPosition,
        muzzleNode.GlobalPosition + Transform.X * 1000,
        2147483647, // collision mask
        new Godot.Collections.Array<Rid> { }
    );
    var result = SpaceState.IntersectRay(physicsRayQueryParameters);
    Vector2 rayCastPosition = (Vector2)result["position"];
    Vector2 pos = rayCastPosition * Transform;
    beamNode.SetPointPosition(1, pos);
    beamBgNode.SetPointPosition(1, pos);
    particlesNode.Position = pos;
    return result;
  }

  public override void _PhysicsProcess(double delta) {
    if (Engine.IsEditorHint()) {
      return;
    }

    var castResult = CastBeam();
    var collider = castResult["collider"].As<Node>();

    if (collider != null && collider is BoxFace boxFace) {
      var groups = collider.GetGroups();
      if (groups.Count == 1 && groups[0] == color_group) {
        // Play some SFX maybe?
      }
      else {
        Event.Instance.EmitPlayerDying(null, GlobalPosition, Constants.EntityType.LAZER);
      }
    }
  }
}
