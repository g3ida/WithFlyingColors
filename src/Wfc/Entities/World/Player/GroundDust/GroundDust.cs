namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;
using Wfc.Utils.Layers;

// The dust the cube kicks off the ground, in the colour of the ground it came off: a trail behind
// it while it walks, and a burst under it where it lands. Faint on purpose - that colour is what
// the player is reading to stay alive, and a solid puff over it would be one more coloured thing
// to have to look past.
//
// Both emitters are top level, so where the dust leaves the ground and which way it is thrown are
// plain world terms however the cube happens to be turned, and the dust already in the air stays
// where it was left while the cube rolls on over it.
[ScenePath]
public partial class GroundDust : Node2D {
  #region Constants
  // Below this share of the cube's own run speed it is drifting to a stop rather than walking.
  private const float WALK_SPEED_SHARE = 0.3f;

  // Where the trail leaves the ground behind the cube, as a share of the cube's half width.
  private const float TRAIL_OFFSET = 0.5f;

  // How steeply the trail is thrown back off the ground.
  private const float RISE = 0.22f;

  // The fall a landing has to be to raise anything, and the fall that raises the whole of it.
  // Short of the first the cube has stepped down rather than come down.
  private const float SOFT_LANDING_SPEED = 250.0f;
  private const float HARD_LANDING_SPEED = 1100.0f;

  // What the softest landing that raises dust at all throws, as a share of the hardest.
  private const float SOFT_LANDING_THROW = 0.45f;

  // How far out along the underside the landing burst leaves the cube, as a share of its half
  // width. It squirts out from the two bottom corners rather than up off the whole underside:
  // the cube is wider than any puff thrown up under it, so that one is never seen at all.
  private const float LANDING_VENT_SHARE = 0.9f;

  // What the ground's colour is worth at its strongest; the scene's gradient fades it out from
  // there. The one knob that makes the whole effect fainter without disturbing that fade.
  private const float OPACITY = 0.3f;

  // How far past the cube's underside the ground is looked for.
  private const float PROBE_DEPTH = 12.0f;

  // The ground is sampled on a timer rather than every tick: a walkable coloured run is a single
  // colour along its whole length, so what is underfoot only changes across a jump - and a
  // landing takes its own reading anyway.
  private const float SAMPLE_INTERVAL = 0.2f;
  #endregion Constants

  #region Nodes
  [NodePath("Walk")]
  private CpuParticles2D _walkNode = default!;
  [NodePath("Landing")]
  private CpuParticles2D _landingNode = default!;
  #endregion Nodes

  private Player _cube = default!;
  private PhysicsRayQueryParameters2D? _groundProbe;
  private float _sinceSample = SAMPLE_INTERVAL;
  private bool _wasOnFloor = true;
  private float _fallSpeed;
  // Paired with the outward normals the scene gives them, one vent to each bottom corner.
  private readonly Vector2[] _landingVents = new Vector2[2];
  private float _landingThrowMin;
  private float _landingThrowMax;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _cube = GetParent<Player>();
    // The authored throw is the hardest landing's; every softer one is scaled off it.
    _landingThrowMin = _landingNode.InitialVelocityMin;
    _landingThrowMax = _landingNode.InitialVelocityMax;
    _walkNode.Emitting = false;
  }

  public override void _PhysicsProcess(double delta) {
    base._PhysicsProcess(delta);
    if (!IsInstanceValid(_cube)) {
      return;
    }

    var onFloor = _cube.IsOnFloor();
    if (onFloor && !_wasOnFloor && !_cube.IsDying()) {
      _burstOnLanding();
    }
    _wasOnFloor = onFloor;
    // Held from the tick before the contact: by the time the floor is under the cube the fall
    // that brought it there has already been taken out of its velocity.
    _fallSpeed = onFloor ? 0.0f : _cube.Velocity.Y;

    _stepTrail((float)delta);
  }

  private void _stepTrail(float delta) {
    if (!_isWalking()) {
      _walkNode.Emitting = false;
      // So the next step is drawn in the colour of wherever it is taken rather than of wherever
      // the last one ended.
      _sinceSample = SAMPLE_INTERVAL;
      return;
    }

    _sinceSample += delta;
    if (_sinceSample >= SAMPLE_INTERVAL) {
      _sinceSample = 0.0f;
      _takeGroundColor();
    }

    var half = _cube.GetCollisionHalfExtents();
    var back = _cube.Velocity.X < 0.0f ? 1.0f : -1.0f;
    _walkNode.GlobalPosition = _cube.GlobalPosition + new Vector2(back * half.X * TRAIL_OFFSET, half.Y);
    _walkNode.Direction = new Vector2(back, -RISE).Normalized();
    if (!_walkNode.Emitting) {
      // The first step of a walk is a teleport from wherever the last one ended.
      _walkNode.ResetPhysicsInterpolation();
      _walkNode.Emitting = true;
    }
  }

  // A landing spreads both ways from where the cube came down: it has no direction of travel to
  // trail behind, the way a step does.
  private void _burstOnLanding() {
    if (_fallSpeed < SOFT_LANDING_SPEED) {
      return;
    }
    _takeGroundColor();

    var half = _cube.GetCollisionHalfExtents();
    var force = Mathf.Lerp(SOFT_LANDING_THROW, 1.0f, Mathf.Clamp(
      Mathf.InverseLerp(SOFT_LANDING_SPEED, HARD_LANDING_SPEED, _fallSpeed), 0.0f, 1.0f));

    _landingNode.GlobalPosition = _cube.GlobalPosition + new Vector2(0.0f, half.Y);
    _landingVents[0] = new Vector2(-half.X * LANDING_VENT_SHARE, 0.0f);
    _landingVents[1] = new Vector2(half.X * LANDING_VENT_SHARE, 0.0f);
    _landingNode.EmissionPoints = _landingVents;
    _landingNode.InitialVelocityMin = _landingThrowMin * force;
    _landingNode.InitialVelocityMax = _landingThrowMax * force;
    // The emitter has been sitting where the cube last came down - or, before the first landing,
    // at the level's origin. Interpolated, the burst is drawn on its way over from there rather
    // than under the cube that raised it.
    _landingNode.ResetPhysicsInterpolation();
    _landingNode.Restart();
  }

  private bool _isWalking() =>
    _cube.IsStanding()
    && _cube.IsOnFloor()
    && Mathf.Abs(_cube.Velocity.X) > _cube.SpeedLimit * WALK_SPEED_SHARE;

  // Neutral ground, and ground carrying no colour at all, both leave the dust white: there is no
  // colour to take from a surface every face is safe on.
  private void _takeGroundColor() {
    var group = _groundColorGroup();
    var tint = group is null
      ? Colors.White
      : SkinManager.Instance.CurrentSkin.GetColor(
        GameSkin.ColorGroupToSkinColor(group), SkinColorIntensity.Basic);
    var color = new Color(tint, OPACITY);
    _walkNode.Color = color;
    _landingNode.Color = color;
  }

  // Asked of the colour area rather than of the body under it: colour is carried by areas
  // everywhere in the game, and the body a tilemap or a heap of blocks presents has none.
  private string? _groundColorGroup() {
    _groundProbe ??= new PhysicsRayQueryParameters2D {
      CollisionMask = PhysicsLayers.Platform.Mask,
      CollideWithAreas = true,
      CollideWithBodies = false,
    };
    _groundProbe.From = _cube.GlobalPosition;
    _groundProbe.To = _cube.GlobalPosition
      + (Vector2.Down * (_cube.GetCollisionHalfExtents().Y + PROBE_DEPTH));

    using var hit = _cube.GetWorld2D().DirectSpaceState.IntersectRay(_groundProbe);
    return hit.Count > 0 && hit["collider"].As<Node>() is { } area
      ? ColorUtils.OwnColorGroupOf(area)
      : null;
  }
}
