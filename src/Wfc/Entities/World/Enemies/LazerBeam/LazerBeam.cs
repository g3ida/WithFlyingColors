namespace Wfc.Entities.World.Enemies;

using Godot;
using Wfc.Entities.World;
using Wfc.Entities.World.Player;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[Tool]
[ScenePath]
public partial class LazerBeam : Node2D {
  #region Nodes
  [NodePath("Line2D")]
  private Line2D beamNode = default!;
  [NodePath("Line2DBackground")]
  private Line2D beamBgNode = default!;
  [NodePath("Muzzle")]
  private Marker2D muzzleNode = default!;
  [NodePath("Particles")]
  private CpuParticles2D particlesNode = default!;
  [NodePath("Base")]
  private Sprite2D baseNode = default!;
  #endregion Nodes

  [Export]
  public string ColorGroup { get; set; } = "blue";

  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    Color color = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(ColorGroup),
      SkinColorIntensity.Basic
    );
    Color darkColor = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(ColorGroup),
      SkinColorIntensity.Dark
    );
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
      if (groups.Count == 1 && groups[0] == ColorGroup) {
        // FIXME: Play some SFX maybe?
      }
      else {
        EventHandler.Instance.EmitPlayerDying(GlobalPosition, EntityType.Lazer);
      }
    }
  }
}
