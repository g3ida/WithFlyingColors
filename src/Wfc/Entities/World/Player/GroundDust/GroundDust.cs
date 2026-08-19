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
//
// Left where it was is left where the world was, so dust raised off ground that is itself going
// somewhere is thrown with that ground's motion on top of its own: without it a platform walks out
// from under its own dust and leaves it hanging in the air behind.
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

  // How steeply a vent squirts out along the ground, as a rise over its outward run.
  private const float VENT_RISE = 0.29f;

  // One vent to each bottom corner, aimed out and up before the ground's own motion is put on it.
  private static readonly Vector2[] LANDING_AIM = [
    new Vector2(-1.0f, -VENT_RISE).Normalized(),
    new Vector2(1.0f, -VENT_RISE).Normalized(),
  ];

  // What the ground's colour is worth at its strongest; the scene's gradient fades it out from
  // there. The one knob that makes the whole effect fainter without disturbing that fade.
  private const float OPACITY = 0.3f;

  // How far past the cube's underside the ground is looked for.
  private const float PROBE_DEPTH = 12.0f;

  // The ground is sampled on a timer rather than every tick: a walkable coloured run is a single
  // colour along its whole length, so what is underfoot only changes across a jump - and a
  // landing takes its own reading anyway.
  private const float SAMPLE_INTERVAL = 0.2f;

  // How square to the cube's up a contact has to stand to be the floor it walks on rather than a
  // wall it is pressed against.
  private const float FLOOR_DOT = 0.7f;
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
  private readonly Vector2[] _landingVents = new Vector2[LANDING_AIM.Length];
  private readonly Vector2[] _landingNormals = new Vector2[LANDING_AIM.Length];
  private float _walkThrowMin;
  private float _walkThrowMax;
  private float _landingThrowMin;
  private float _landingThrowMax;
  private Node2D? _ground;
  private Vector2 _groundAt;
  private Vector2 _groundVelocity;
  // The fall a landing was made at and where it was made, held over the tick between the contact
  // and the burst it raises. A fall of nothing is no burst owed.
  private float _pendingLanding;
  private Vector2 _pendingLandingAt;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _cube = GetParent<Player>();
    _walkThrowMin = _walkNode.InitialVelocityMin;
    _walkThrowMax = _walkNode.InitialVelocityMax;
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

    _readGroundMotion((float)delta);

    var onFloor = _cube.IsOnFloor();
    if (_pendingLanding > 0.0f && !_cube.IsDying()) {
      // Held over a tick, so the contact point has to be carried on by a tick of the ground's own
      // travel to still be the spot the cube came down on.
      _burstOnLanding(_pendingLanding, _pendingLandingAt + (_groundVelocity * (float)delta));
    }
    // A burst is owed a tick, not raised on the spot: the ground's speed is read from how far it
    // has moved since the tick before, and one the cube has only just met has not been seen move.
    _pendingLanding = 0.0f;
    if (onFloor && !_wasOnFloor) {
      _pendingLanding = _fallSpeed;
      _pendingLandingAt = _cube.GlobalPosition;
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
    var thrown = _carriedByGround(new Vector2(back, -RISE).Normalized(), _walkThrowMin, _walkThrowMax);
    _walkNode.Direction = thrown.Aim;
    _walkNode.InitialVelocityMin = thrown.Min;
    _walkNode.InitialVelocityMax = thrown.Max;
    if (!_walkNode.Emitting) {
      // The first step of a walk is a teleport from wherever the last one ended.
      _walkNode.ResetPhysicsInterpolation();
      _walkNode.Emitting = true;
    }
  }

  // A landing spreads both ways from where the cube came down, and stays there: it has no
  // direction of travel to trail behind, the way a step does, and the cube runs on out of it.
  private void _burstOnLanding(float fallSpeed, Vector2 at) {
    if (fallSpeed < SOFT_LANDING_SPEED) {
      return;
    }
    _takeGroundColor();

    var half = _cube.GetCollisionHalfExtents();
    var force = Mathf.Lerp(SOFT_LANDING_THROW, 1.0f, Mathf.Clamp(
      Mathf.InverseLerp(SOFT_LANDING_SPEED, HARD_LANDING_SPEED, fallSpeed), 0.0f, 1.0f));

    _landingNode.GlobalPosition = at + new Vector2(0.0f, half.Y);
    _landingVents[0] = new Vector2(-half.X * LANDING_VENT_SHARE, 0.0f);
    _landingVents[1] = new Vector2(half.X * LANDING_VENT_SHARE, 0.0f);
    _landingNode.EmissionPoints = _landingVents;
    _aimVents(_landingThrowMin * force, _landingThrowMax * force);
    // The emitter has been sitting where the cube last came down - or, before the first landing,
    // at the level's origin. Interpolated, the burst is drawn on its way over from there rather
    // than under the cube that raised it.
    _landingNode.ResetPhysicsInterpolation();
    _landingNode.Restart();
  }

  // Each vent leans by the ground's motion on its own, but the emitter keeps one spread of speeds
  // for all of them, so what they share is the mean of what each asked for.
  private void _aimVents(float min, float max) {
    var speedMin = 0.0f;
    var speedMax = 0.0f;
    for (var i = 0; i < LANDING_AIM.Length; i++) {
      var thrown = _carriedByGround(LANDING_AIM[i], min, max);
      _landingNormals[i] = thrown.Aim;
      speedMin += thrown.Min / LANDING_AIM.Length;
      speedMax += thrown.Max / LANDING_AIM.Length;
    }
    _landingNode.EmissionNormals = _landingNormals;
    _landingNode.InitialVelocityMin = speedMin;
    _landingNode.InitialVelocityMax = speedMax;
  }

  // The emitter throws every particle one way at a spread of speeds, and has no term for adding
  // the ground's own motion on top of that. Leaning the one direction by it and shifting the whole
  // spread with it comes to the same thing, and keeps the range the throw was authored with.
  private (Vector2 Aim, float Min, float Max) _carriedByGround(Vector2 aim, float min, float max) {
    var middle = (aim * (min + max) * 0.5f) + _groundVelocity;
    var speed = middle.Length();
    if (Mathf.IsZeroApprox(speed)) {
      return (aim, 0.0f, 0.0f);
    }
    var spread = (max - min) * 0.5f;
    return (middle / speed, Mathf.Max(speed - spread, 0.0f), speed + spread);
  }

  // A platform that moves by its own transform reports no velocity of its own, so the floor's is
  // measured from how far it has travelled since the tick before. The floor is kept while the cube
  // stands on it: a tick that reports no contact is not the cube stepping off.
  private void _readGroundMotion(float delta) {
    var ground = _floorUnderCube() ?? (_cube.IsOnFloor() ? _ground : null);
    if (ground is null || !IsInstanceValid(ground)) {
      _ground = null;
      _groundVelocity = Vector2.Zero;
      return;
    }

    var at = ground.GlobalPosition;
    var moved = at - _groundAt;
    // Ground that has covered more than the cube's own half width in a single tick has been put
    // there rather than travelled there - a level reset, or a stack of blocks dropping a row.
    var travelled = ground == _ground
      && delta > 0.0f
      && moved.Length() <= _cube.GetCollisionHalfExtents().X;
    _groundVelocity = travelled ? moved / delta : Vector2.Zero;
    _ground = ground;
    _groundAt = at;
  }

  private Node2D? _floorUnderCube() {
    for (var i = 0; i < _cube.GetSlideCollisionCount(); i++) {
      var contact = _cube.GetSlideCollision(i);
      if (contact.GetNormal().Dot(_cube.UpDirection) >= FLOOR_DOT && contact.GetCollider() is Node2D floor) {
        return floor;
      }
    }
    return null;
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
