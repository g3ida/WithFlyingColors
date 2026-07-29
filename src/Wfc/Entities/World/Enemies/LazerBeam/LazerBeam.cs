namespace Wfc.Entities.World.Enemies;

using Godot;
using Wfc.Entities.World;
using Wfc.Entities.World.Player;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Layers;
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

  private bool _wasBurning;

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

  private const float BEAM_RANGE = 1000.0f;

  // How far into its own mount the beam senses faces.
  //
  // A beam sits flush on the surface it fires from, and a player's faces reach slightly inside
  // whatever they rest on - that overlap is how standing on a colored surface is detected at all.
  // So the face presented to the beam is behind the muzzle, where no ray cast from the muzzle can
  // see it, and the beam answers with the face on the far side of the cube instead.
  //
  // Must exceed how far a face reaches past the surface it rests on, and stay under how deep the
  // emitter may reach into its mount, or a face on the far side of a thin one answers instead.
  // Reaching back is otherwise free: the query takes areas on the face layer only, and no face is
  // ever inside solid ground.
  private const float MOUNT_CLEARANCE = 24.0f;

  // What stops the beam: the solid world. The player is a body too, but the cube's hull is
  // not what the beam cares about - the color it presents is, and that lives on the face
  // areas, which a bodies-only ray cannot see.
  private static readonly uint SOLID_MASK = PhysicsLayers.Default.Mask | PhysicsLayers.Platform.Mask;

  public PhysicsDirectSpaceState2D SpaceState => GetWorld2D().DirectSpaceState;

  // Normalized, because Transform.X carries the node's scale and a scaled beam would otherwise
  // read as a longer or shorter one.
  private Vector2 _beamDirection => Transform.X.Normalized();

  // How far the solid world lets the beam travel. Bodies only, so a trigger volume or a gem in
  // the way does not cut the beam short.
  private Vector2 _castToWorld() {
    var from = muzzleNode.GlobalPosition;
    var to = from + _beamDirection * BEAM_RANGE;

    var query = PhysicsRayQueryParameters2D.Create(from, to, SOLID_MASK);
    query.CollideWithAreas = false;
    var result = SpaceState.IntersectRay(query);

    // IntersectRay returns an empty dictionary when it hits nothing, so the endpoint has to
    // be defaulted rather than read out of it - this used to throw every physics frame the
    // beam pointed at open sky.
    return result.ContainsKey("position") ? (Vector2)result["position"] : to;
  }

  // The face the beam lands on and where it lands, if it reaches one before the world stops it.
  // Areas only and masked to the face layer: the default Create() leaves CollideWithAreas false,
  // which is why `collider is BoxFace` could never be true and the beam has been decorative since
  // the port.
  private (BaseFace Face, Vector2 Position)? _castToFace(Vector2 worldEnd) {
    var from = muzzleNode.GlobalPosition - _beamDirection * MOUNT_CLEARANCE;
    var query = PhysicsRayQueryParameters2D.Create(from, worldEnd, PhysicsLayers.BoxFace.Mask);
    query.CollideWithBodies = false;
    query.CollideWithAreas = true;
    var result = SpaceState.IntersectRay(query);

    // An empty dictionary means the ray reached the end of the segment, and indexing a Godot
    // dictionary that does not hold the key throws rather than handing back nil.
    if (!result.ContainsKey("collider") || result["collider"].As<Node>() is not BaseFace face) {
      return null;
    }
    return (face, (Vector2)result["position"]);
  }

  // The beam is absorbed by whatever it lands on, so it is drawn only that far. A face resting on
  // the mount lands behind the emitter: clamping reads that as the beam blocked at its base, which
  // is what standing on the emitter looks like.
  private void _drawBeamTo(Vector2 endPoint) {
    var from = muzzleNode.GlobalPosition;
    var along = Mathf.Max((endPoint - from).Dot(_beamDirection), 0.0f);
    var localEnd = (from + _beamDirection * along) * Transform;

    beamNode.SetPointPosition(1, localEnd);
    beamBgNode.SetPointPosition(1, localEnd);
    particlesNode.Position = localEnd;
  }

  public override void _PhysicsProcess(double delta) {
    if (Engine.IsEditorHint()) {
      return;
    }

    var worldEnd = _castToWorld();
    var hit = _castToFace(worldEnd);

    // Ending on the player, not on the wall behind them.
    _drawBeamTo(hit?.Position ?? worldEnd);

    // The beam crossing a face it burns is one event, not one per frame it goes on crossing it.
    var burns = hit is { } landing && !landing.Face.AcceptsColor(ColorGroup);
    if (burns && !_wasBurning) {
      EventHandler.Instance.EmitPlayerDying(GlobalPosition, EntityType.Lazer);
    }
    _wasBurning = burns;
  }
}
