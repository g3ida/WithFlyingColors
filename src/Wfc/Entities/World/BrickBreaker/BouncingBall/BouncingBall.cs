namespace Wfc.Entities.World.BrickBreaker;

using System.Linq;
using Godot;
using Wfc.Entities.World;
using Wfc.Entities.World.Player;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;
using Wfc.Utils.Layers;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
public partial class BouncingBall : CharacterBody2D {
  #region Exported
  [Export]
  public string ColorGroup = ColorUtils.BLUE;
  #endregion Exported

  #region Constants
  private const float SPEED = 420.0f;
  private const float SPEED_UNIT = 60.0f;
  private static readonly Vector2 SPAWN_DIRECTION = Vector2.Up;
  private const float SPAWN_DIRECTION_RANDOM_DEGREES = 45.0f;

  private const string WALL_GROUP = "wall";

  // Breakout's paddle rule: where along the struck face the ball lands is how far it leaves from
  // that face's normal, and it is the only aim the player is given.
  private const float MAX_DEFLECTION_DEGREES = 60.0f;

  // A ball leaving a wall along that wall's normal comes back to the same spot forever.
  private const float GRAZING_DEGREES = 5.0f;
  private const float GRAZING_CORRECTION_DEGREES = 10.0f;
  #endregion Constants

  #region Nodes
  [NodePath("Area2D")]
  private Area2D _areaNode = default!;
  [NodePath("BBSpr")]
  private BouncingBallSprite _spriteNode = default!;
  [NodePath("CollisionShape2D")]
  private CollisionShape2D _bodyCollisionShape = default!;
  #endregion Nodes

  // Direction and speed are held apart so the speed cannot be mangled by a bounce writing a whole
  // velocity. The base is what the ball settles back to; a hit from a moving paddle can leave it
  // traveling faster until it meets something else.
  private Vector2 _direction = SPAWN_DIRECTION;
  private float _baseSpeed = SPEED;
  private float _speed = SPEED;

  public Vector2 BallVelocity => _direction * _speed;

  public Area2D? DeathZone = null; // FIXME: this is set in breakBreaker. better logic ?

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    GD.Randomize();
    _reset();
    SetColor(ColorGroup);
  }

  public void SetColor(string colorName) {
    _spriteNode.SetColor(colorName);
    if (_areaNode.IsInGroup(ColorGroup)) {
      _areaNode.RemoveFromGroup(ColorGroup);
    }
    _areaNode.AddToGroup(colorName);
    ColorGroup = colorName;
  }

  // Aims a ball that has not run its _Ready yet, which is where a ball is given its random spawn
  // direction. Reached through CallDeferred by the triple-ball power-up for that reason.
  public void SetBallVelocity(Vector2 velocity) {
    if (velocity.LengthSquared() <= MathUtils.EPSILON) {
      return;
    }
    _direction = velocity.Normalized();
    _speed = _baseSpeed;
  }

  public void IncrementSpeed() {
    _baseSpeed += SPEED_UNIT;
    _speed = _baseSpeed;
  }

  public override void _PhysicsProcess(double delta) {
    // The paddle is resolved by hand, not by the sweep below, because the paddle is deliberately not
    // something the ball can collide with. A cube climbing into the ball buries it deeper than its own
    // radius in one frame, and a body that overlaps another cannot be swept out of it: the ball's
    // motion went dead and it traveled at the cube's speed instead of its own for as long as the jump
    // lasted. Walls and bricks still block it - they are meant to.
    _offPaddle();

    var collision = MoveAndCollide(BallVelocity * (float)delta, recoveryAsCollision: true);
    if (collision == null) {
      return;
    }

    // One contact per frame, left resting against what it hit. Breaking a brick, taking its color
    // and the wrong-color death all run off the overlap pass on this ball's Area2D, which sees only
    // where the ball came to rest, and every area shape is slightly larger than its own body so
    // that bodies in contact are already overlapping. Spending the rest of the frame's travel
    // carried the ball back out before the pass ran.
    _resolve(collision);
  }

  private void _resolve(KinematicCollision2D collision) {
    var normal = collision.GetNormal();

    // Out of what was hit before deciding where to go. Godot's own recovery only nudges a body out of
    // a shallow overlap, and a moving obstacle can bury the ball further than that in a single frame.
    GlobalPosition += normal * collision.GetDepth();

    // Whatever the paddle lent the ball is spent on the next thing it meets.
    _speed = _baseSpeed;

    var bounced = _bounceOff(normal);
    _direction = _isWall(collision) ? _breakGrazingLoop(bounced, normal) : bounced;
  }

  // Reflected in the paddle's own frame of reference, which is the whole of what a moving paddle does
  // to a ball. A static mirror could only send the ball away at its own speed, so a cube jumping faster
  // than the ball could never be escaped at all.
  private void _offPaddle() {
    if (!_paddleInReach(out var player)) {
      return;
    }

    var paddle = player.Velocity;
    var approach = BallVelocity - paddle;
    if (!_contactWith(player, approach, out var contact)) {
      return;
    }

    // Out of the cube first, whatever comes next: a cube climbing into the ball buries it deeper than
    // its own radius in a frame, and a ball left inside is a ball the cube carries around with it.
    GlobalPosition += contact.Normal * contact.Depth;

    // Judged by where the cube's surface met the ball. Reading the color off the two centers instead
    // let the far side of the cube condemn a ball the near side had just struck, which is exactly what
    // a dash looks like from the inside - and it is the one hit the player aims deliberately.
    if (!player.AcceptsColorAt(contact.Point, ColorGroup)) {
      EventHandler.Instance.EmitPlayerDying(_areaNode, GlobalPosition, EntityType.Ball);
      return;
    }

    // Only a ball the cube is closing on is struck. Taken from the two velocities rather than the two
    // positions, which stop meaning "towards" the moment a dash carries the cube past the ball.
    if (approach.Dot(contact.Normal) >= 0.0f) {
      return;
    }

    var normal = contact.Normal;

    // How hard the paddle is driving into the ball, along the face it presents. A paddle that just
    // stands there leaves the aim entirely to the player; one that is jumping throws the ball off its
    // face instead. That is what a slam should look like, and it is also what stops the aim trading
    // away the speed along the normal - the part the ball needs to outpace the face.
    var closing = Mathf.Max(paddle.Dot(normal), 0.0f);
    var aimWeight = _baseSpeed / (_baseSpeed + closing);
    var deflection = _faceOffsetOf(player, contact.Point, normal) * aimWeight * Mathf.DegToRad(MAX_DEFLECTION_DEGREES);
    var aim = normal.Rotated(deflection);

    // The paddle lends at most its own speed, bounded in its own frame. Bounding the world velocity
    // instead scaled the escape back out of it again.
    var lent = Mathf.Min(approach.Length(), _baseSpeed + closing);
    var outgoing = (aim * lent) + paddle;

    _direction = outgoing.LengthSquared() > MathUtils.EPSILON ? outgoing.Normalized() : aim;
    _speed = Mathf.Clamp(outgoing.Length(), _baseSpeed, _baseSpeed + closing);
  }

  // Found with a live query rather than from this ball's own overlap list, which is a physics frame
  // stale: a dashing cube crosses most of its own width in that time, so by the time the list catches
  // up the ball is already past the face that should have struck it.
  private bool _paddleInReach(out Player player) {
    var query = new PhysicsShapeQueryParameters2D {
      ShapeRid = _bodyCollisionShape.Shape.GetRid(),
      Transform = new Transform2D(0.0f, Scale, 0.0f, GlobalPosition),
      CollisionMask = PhysicsLayers.Player.Mask,
      CollideWithBodies = true,
      CollideWithAreas = false,
    };

    foreach (var hit in GetWorld2D().DirectSpaceState.IntersectShape(query, 1)) {
      if (hit["collider"].As<GodotObject>() is Player found) {
        player = found;
        return true;
      }
    }
    player = null!;
    return false;
  }

  // Against the cube's outer surface, in the cube's own frame so that a rotated cube needs no special
  // case, and back into world space for whoever reads the contact.
  private bool _contactWith(Player player, Vector2 approach, out BoxContact contact) {
    var facing = player.GlobalRotation;
    var local = (GlobalPosition - player.GlobalPosition).Rotated(-facing);

    if (!BoxContact.Find(local, _radius(), player.GetCollisionHalfExtents(), approach.Rotated(-facing), out var found)) {
      contact = default;
      return false;
    }

    contact = new BoxContact(
      found.Normal.Rotated(facing),
      player.GlobalPosition + found.Point.Rotated(facing),
      found.Depth
    );
    return true;
  }

  private float _radius() => ((_bodyCollisionShape.Shape as CircleShape2D)?.Radius ?? 0.0f) * Scale.X;

  // Where along the struck face the ball landed, from one end of it to the other.
  private static float _faceOffsetOf(Player player, Vector2 contactPoint, Vector2 normal) {
    var alongFace = new Vector2(-normal.Y, normal.X);
    var half = player.GetCollisionHalfExtents();
    var reach = (Mathf.Abs(alongFace.X) * half.X) + (Mathf.Abs(alongFace.Y) * half.Y);
    if (reach <= MathUtils.EPSILON) {
      return 0.0f;
    }
    return Mathf.Clamp((contactPoint - player.GlobalPosition).Dot(alongFace) / reach, -1.0f, 1.0f);
  }

  // Only a ball heading into the surface is reflected: depenetration reports a contact for one that
  // is already leaving, and reflecting that would drive it back in.
  private Vector2 _bounceOff(Vector2 normal) =>
    _direction.Dot(normal) < 0.0f ? _direction.Bounce(normal).Normalized() : _direction;

  // One rule for both wall orientations: a ball leaving along the normal is about to retrace its
  // path, so tilt it away, keeping the drift it already had.
  private static Vector2 _breakGrazingLoop(Vector2 direction, Vector2 normal) {
    var offNormal = normal.AngleTo(direction);
    if (Mathf.Abs(offNormal) > Mathf.DegToRad(GRAZING_DEGREES)) {
      return direction;
    }

    var tilt = Mathf.DegToRad((float)GD.RandRange(GRAZING_DEGREES, GRAZING_CORRECTION_DEGREES));
    return direction.Rotated(offNormal >= 0.0f ? tilt : -tilt);
  }

  private static bool _isWall(KinematicCollision2D collision) =>
    collision.GetCollider() is Node2D collider && collider.IsInGroup(WALL_GROUP);


  private void _reset() {
    var randomness = (float)GD.RandRange(-SPAWN_DIRECTION_RANDOM_DEGREES, SPAWN_DIRECTION_RANDOM_DEGREES);
    _direction = SPAWN_DIRECTION.Rotated(Mathf.DegToRad(randomness));
    _baseSpeed = SPEED;
    _speed = SPEED;
  }

  private static bool IsProbablyABrick(Area2D area, Godot.Collections.Array<StringName> groups) {
    var isBoxFace = area is BaseFace; // can also be: area.GetParent<Player>() != null;
    return !isBoxFace && groups.Count > 0;
  }

  private void _on_Area2D_area_entered(Area2D area) {
    if (area == DeathZone) {
      EventHandler.Instance.EmitBouncingBallRemoved(this);
      QueueFree();
      return;
    }
    var groups = area.GetGroups();
    if (IsProbablyABrick(area, groups)) {
      if (ColorUtils.COLOR_GROUPS.Contains((string)groups[0])) {
        var current_groups = _areaNode.GetGroups();
        foreach (var group in current_groups) {
          _areaNode.RemoveFromGroup((string)group);
        }
        _areaNode.AddToGroup((string)groups[0]);
        ColorGroup = (string)groups[0];
        _spriteNode.SetColor((string)groups[0]);
      }
    }
  }

}
