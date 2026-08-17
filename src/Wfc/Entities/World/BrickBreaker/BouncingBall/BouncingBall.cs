namespace Wfc.Entities.World.BrickBreaker;

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Godot;
using Wfc.Entities.World;
using Wfc.Entities.World.Player;
using Wfc.Screens.Levels;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;
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

  // How long a paddle's blow keeps half of whatever speed it lent. Dropping the lot at the next
  // thing the ball meets read as the ball hitting treacle, since that is usually the very first
  // wall it reaches. The floor is what carries the last of the loan away: halving alone leaves a
  // tail the player cannot see and the ball never settles.
  private const float LENT_SPEED_HALF_LIFE = 0.5f;
  private const float LENT_SPEED_FLOOR = SPEED_UNIT;
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
  // traveling faster, and that surplus bleeds off over time rather than at the next contact.
  private Vector2 _direction = SPAWN_DIRECTION;
  private float _baseSpeed = SPEED;
  private float _speed = SPEED;

  // The side of the cube this ball is resting against, in world space, held for as long as the two
  // stay in touch. Once the cube has the ball inside it, its position no longer says which side it
  // came in through, and a cube stopped dead against a wall has no motion left to say either: the
  // ball's own travel then reads as one arriving from the far side, and out it went through the back
  // of the cube.
  private Vector2? _restingOn;

  // Whether the contact the ball is currently in has already been reported as fatal, so that
  // resting against a face the cube cannot take reports one death rather than one a frame.
  private bool _wasFatalContact;

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
    _speed = Mathf.Max(_speed, _baseSpeed);
  }

  public override void _PhysicsProcess(double delta) {
    // Before the paddle is asked for more, so a ball struck this frame keeps the whole of what it
    // was just given.
    _spendLentSpeed((float)delta);

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

    var bounced = _bounceOff(normal);
    _direction = _isWall(collision) ? _breakGrazingLoop(bounced, normal) : bounced;
  }

  // Reflected in the paddle's own frame of reference, which is the whole of what a moving paddle does
  // to a ball. A static mirror could only send the ball away at its own speed, so a cube jumping faster
  // than the ball could never be escaped at all.
  private void _offPaddle() {
    if (!_hasPaddle(out var player)) {
      _restingOn = null;
      _wasFatalContact = false;
      return;
    }

    var paddle = player.Velocity;
    var approach = BallVelocity - paddle;
    if (!_contactWith(player, approach, out var contact)) {
      _restingOn = null;
      _wasFatalContact = false;
      return;
    }
    _restingOn = contact.Normal;

    // Out of the cube first, whatever comes next: a cube climbing into the ball buries it deeper than
    // its own radius in a frame, and a ball left inside is a ball the cube carries around with it.
    _separateFrom(player, contact);

    // Judged by where the cube's surface met the ball. Reading the color off the two centers instead
    // let the far side of the cube condemn a ball the near side had just struck, which is exactly what
    // a dash looks like from the inside - and it is the one hit the player aims deliberately.
    //
    // Reported once per contact, and again if the cube turns a face the ball cannot take onto a ball
    // it is already resting against, which is a fresh way for the same touch to become fatal.
    if (!player.AcceptsColorAt(contact.Point, ColorGroup)) {
      if (!_wasFatalContact) {
        _wasFatalContact = true;
        EventHandler.Instance.EmitPlayerDying(_areaNode, GlobalPosition, EntityType.Ball);
      }
      return;
    }
    _wasFatalContact = false;

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
    // instead scaled the escape back out of it again. A surplus the ball is still carrying raises
    // that bound rather than being cut by it, so a slower paddle cannot undo an earlier slam.
    var ceiling = Mathf.Max(_speed, _baseSpeed + closing);
    var lent = Mathf.Min(approach.Length(), ceiling);
    var outgoing = (aim * lent) + paddle;

    _direction = outgoing.LengthSquared() > MathUtils.EPSILON ? outgoing.Normalized() : aim;
    _speed = Mathf.Clamp(outgoing.Length(), _baseSpeed, ceiling);
  }

  // Halved on a fixed clock rather than shed at a fixed rate, so a hard slam loses its edge quickly
  // and a gentle nudge still lingers for about as long.
  private void _spendLentSpeed(float delta) {
    var surplus = _speed - _baseSpeed;
    if (surplus <= 0.0f) {
      _speed = _baseSpeed;
      return;
    }
    var halved = _baseSpeed + (surplus * Mathf.Pow(0.5f, delta / LENT_SPEED_HALF_LIFE));
    _speed = Mathf.MoveToward(halved, _baseSpeed, LENT_SPEED_FLOOR * delta);
  }

  // Swept rather than teleported, because the room to get clear is not always there: a cube dashing
  // at a wall closes the very gap the ball is sitting in, and a teleport spent that room whether it
  // existed or not - the ball went through the wall and was driven back inside the cube by the
  // recovery, where it stayed for as long as the dash held.
  private void _separateFrom(Player player, BoxContact contact) {
    var crushed = MoveAndCollide(contact.Normal * contact.Depth);
    if (crushed == null) {
      return;
    }

    // The ball has run out of room with the cube still on top of it, so it is the cube that gives
    // way. The ball is what holds that gap open, and a cube that closes it is a cube resting
    // against the wall with the ball somewhere inside it.
    player.MoveAndCollide(-crushed.GetRemainder());
  }

  // Taken straight from the cube rather than asked of the physics server, whose answer - like this
  // ball's own overlap list - is a physics frame behind a paddle that has already moved this frame.
  // A dashing cube crosses most of its own width in that time, so the contact came in a frame late,
  // by which point a cube that has run into a wall stands still and has nothing left to say about
  // which of its faces did the reaching. The contact itself is worked out from the two transforms.
  private static bool _hasPaddle([NotNullWhen(true)] out Player? player) {
    player = GameRepo.Instance.Player.Value;
    return player is not null && GodotObject.IsInstanceValid(player);
  }

  // Against the cube's outer surface, in the cube's own frame so that a rotated cube needs no special
  // case, and back into world space for whoever reads the contact. The side the ball came to rest on
  // outranks where it is heading, which is only evidence of the side it came in through for as long
  // as it is still arriving.
  private bool _contactWith(Player player, Vector2 approach, out BoxContact contact) {
    var facing = player.GlobalRotation;
    var local = (GlobalPosition - player.GlobalPosition).Rotated(-facing);
    var wayOut = (_restingOn ?? -approach).Rotated(-facing);

    if (!BoxContact.Find(local, _radius(), player.GetCollisionHalfExtents(), wayOut, out var found)) {
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

  private void _onAreaEntered(Area2D area) {
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
