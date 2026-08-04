namespace Wfc.Entities.World.Checkpoints;

using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// One bug of a checkpoint's swarm. It is handed an orbit and springs toward the point that orbit
// puts it at, so where it is drawn always lags where it is heading - that lag is what reads as
// flight rather than as a sprite walking a path.
[ScenePath]
public partial class Firefly : Node2D {
  #region Constants
  // How hard the bug is pulled toward its orbit, and how much of that pull it keeps. The spring is
  // deliberately soft: the slack is the flight. Damping only has to keep the overshoot from
  // turning into a wobble it never comes out of.
  private const float HOVER_STIFFNESS = 22.0f;
  private const float HOVER_DAMPING = 6.0f;

  // The gather before the burst. The orbit collapses toward the middle and the pull that carries
  // the bug there hardens, so the swarm bunches rather than drifting in.
  private const float GATHER_STIFFNESS = 90.0f;
  private const float GATHER_DAMPING = 13.0f;
  private const float GATHER_TIGHTNESS = 0.5f;

  private const float BREATH_AMOUNT = 0.22f;
  private const float BOB_AMOUNT = 7.0f;

  // A bug never holds a line for long. The two axes wander at frequencies that never come back
  // into step, so the hover never settles into a circle being traced.
  private const float DART_AMOUNT = 7.0f;
  private const float DART_SPEED = 5.5f;
  private const float DART_RATIO = 1.37f;

  private const float FLEE_SPEED = 700.0f;
  private const float FLEE_RISE = 0.6f;
  // How wide the escape fans out around straight-away-from-the-player. Wide enough that the swarm
  // does not leave as one sheet, narrow enough that nothing doubles back past what startled it.
  private const float FLEE_FAN = 0.85f;
  // Barely any: what a bug leaving has to do is cross the frame, and drag is what stops it.
  private const float FLEE_DRAG = 0.25f;
  private const float FLEE_SWERVE = 700.0f;
  private const float FLEE_SWERVE_SPEED = 6.0f;
  // How long a bug is allowed to keep flying if it somehow never reaches an edge. Leaving is
  // normally the frame taking them, not this - and the fade only exists for the ones it doesn't.
  private const float FLEE_DURATION = 2.1f;
  private const float FLEE_FADE_START = 1.6f;
  // How far past the edge of the frame a bug has to get before it is written off. Enough that it
  // is gone rather than clipped at the boundary.
  private const float OFFSCREEN_MARGIN = 80.0f;

  private const float WING_BEAT = 34.0f;
  private const float WING_FLEE_BEAT_GAIN = 2.2f;
  // The level is seen from the side and so is the bug: the wings stand over the body and open and
  // close above it, rather than reaching out to either side of it the way a bug looked down on
  // would. Measured out from straight up, symmetrically, so there is no side the bug has to be
  // turned around to face.
  private const float WING_SPREAD_CLOSED = 0.18f;
  private const float WING_SPREAD_RANGE = 0.6f;
  private const float WING_FOLD = 0.45f;
  private const float WING_ALPHA = 0.35f;

  private const float GLOW_BASE = 0.3f;
  private const float GLOW_PULSE = 0.2f;
  private const float GLOW_SPEED = 2.6f;
  // Leaving is the one thing the swarm does that the player is meant to look at, so the bugs
  // burn brighter for it from the gather onward.
  private const float GLOW_FLARE = 1.25f;
  private const float GLOW_FLARE_RATE = 9.0f;

  // Below this the heading a velocity gives is noise, and the bug leans on the spot.
  private const float FACING_MIN_SPEED = 8.0f;
  private const float FACING_TURN = 9.0f;
  // How far a bug will lean into a climb or a dive. Past this the wings start coming round under
  // the body and it stops reading as flight.
  private static readonly float MAX_TILT = Mathf.DegToRad(25.0f);
  #endregion Constants

  // The path a bug hovers on, in the swarm's own frame. Flatten squashes the circle so the swarm
  // reads as a cloud rather than a ring, and Phase is what keeps two bugs given the same radius
  // from tracing each other.
  public readonly record struct Orbit(
    float Radius,
    float AngularSpeed,
    float Flatten,
    float Phase,
    float BreathSpeed,
    float BobSpeed
  );

  private enum State { Hovering, Gathering, Fleeing, Gone }

  #region Nodes
  [NodePath("Glow")]
  private Sprite2D _glowNode = default!;
  [NodePath("Body")]
  private Sprite2D _bodyNode = default!;
  [NodePath("WingLeft")]
  private Sprite2D _wingLeftNode = default!;
  [NodePath("WingRight")]
  private Sprite2D _wingRightNode = default!;
  #endregion Nodes

  #region Fields
  private Orbit _orbit = new(Radius: 0.0f, AngularSpeed: 0.0f, Flatten: 1.0f, Phase: 0.0f, BreathSpeed: 0.0f, BobSpeed: 0.0f);
  private Color _bodyColor = Colors.White;
  private Color _glowColor = Colors.White;

  private State _state = State.Hovering;
  private Vector2 _velocity;
  private Vector2 _startledFrom;
  private Vector2 _wingScale = Vector2.One;
  private float _time;
  private float _burstIn;
  private float _fleeTime;
  private float _flare = 1.0f;
  #endregion Fields

  // Called before the bug is in the tree, the way the swarm builds it - everything here is
  // applied once _Ready has the sprites to apply it to.
  public void Configure(Color body, Color glow, Orbit orbit) {
    _bodyColor = body;
    _glowColor = glow;
    _orbit = orbit;
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    _bodyNode.SelfModulate = _bodyColor;
    _wingLeftNode.SelfModulate = new Color(_glowColor, WING_ALPHA);
    _wingRightNode.SelfModulate = _wingLeftNode.SelfModulate;
    _wingScale = _wingLeftNode.Scale;

    _relight();
  }

  // The checkpoint has just been taken. The delay is the swarm's, not the bug's: every bug gathers
  // at once and they leave in a wave. `startledFrom` is where the player came in, in the swarm's
  // frame, and is the only thing that decides which way out any of them takes.
  public void Disperse(float burstIn, Vector2 startledFrom) {
    if (_state == State.Gone) {
      return;
    }
    _state = State.Gathering;
    _burstIn = burstIn;
    _startledFrom = startledFrom;
  }

  // The checkpoint was already taken when the level was loaded, so there was never a swarm here
  // to watch leave.
  public void Extinguish() {
    _state = State.Gone;
    Visible = false;
  }

  public void Relight() {
    if (_state == State.Hovering) {
      return;
    }
    _relight();
  }

  public override void _Process(double delta) {
    if (_state == State.Gone) {
      return;
    }

    var dt = (float)delta;
    _time += dt;

    if (_state == State.Fleeing) {
      _flee(dt);
    }
    else {
      _hover(dt);
    }

    _face(dt);
    _flutter(dt);
  }

  private void _relight() {
    _state = State.Hovering;
    _velocity = Vector2.Zero;
    _fleeTime = 0.0f;
    _flare = 1.0f;
    Position = _orbitPoint(1.0f);
    Rotation = 0.0f;
    Modulate = Colors.White;
    Visible = true;
  }

  private void _hover(float dt) {
    var gathering = _state == State.Gathering;
    if (gathering) {
      _burstIn -= dt;
      if (_burstIn <= 0.0f) {
        _burst();
        return;
      }
    }

    var target = _orbitPoint(gathering ? GATHER_TIGHTNESS : 1.0f);
    var stiffness = gathering ? GATHER_STIFFNESS : HOVER_STIFFNESS;
    var damping = gathering ? GATHER_DAMPING : HOVER_DAMPING;
    _velocity += (((target - Position) * stiffness) - (_velocity * damping)) * dt;
    Position += _velocity * dt;
  }

  private Vector2 _orbitPoint(float tightness) {
    var angle = _orbit.Phase + (_time * _orbit.AngularSpeed);
    var breath = 1.0f + (BREATH_AMOUNT * Mathf.Sin((_time * _orbit.BreathSpeed) + _orbit.Phase));
    var radius = _orbit.Radius * breath * tightness;
    var bob = BOB_AMOUNT * tightness * Mathf.Sin((_time * _orbit.BobSpeed) + _orbit.Phase);
    var dart = new Vector2(
      Mathf.Sin((_time * DART_SPEED) + (_orbit.Phase * 2.0f)),
      Mathf.Cos((_time * DART_SPEED * DART_RATIO) + _orbit.Phase)
    ) * DART_AMOUNT * tightness;
    return new Vector2(Mathf.Cos(angle) * radius, (Mathf.Sin(angle) * radius * _orbit.Flatten) + bob) + dart;
  }

  // Away from whatever walked into them, the way anything startled leaves: nothing flies back over
  // the player. The fan is what keeps that from being one flat sheet of bugs, and it is taken off
  // the orbit so a bug always leaves the same way for the same swarm.
  private void _burst() {
    _state = State.Fleeing;
    _fleeTime = 0.0f;
    var escape = Position - _startledFrom;
    var away = escape.LengthSquared() > MathUtils.EPSILON
      ? escape.Normalized()
      : Vector2.FromAngle(_orbit.Phase);
    away = away.Rotated(FLEE_FAN * Mathf.Sin(_orbit.Phase * 3.0f));
    _velocity = (away + (Vector2.Up * FLEE_RISE)).Normalized() * FLEE_SPEED;
  }

  private void _flee(float dt) {
    _fleeTime += dt;
    // Thrown straight out, four of them at once, reads as a firework. The swerve is what turns it
    // back into bugs deciding where to go.
    var side = new Vector2(-_velocity.Y, _velocity.X).Normalized();
    _velocity += side * FLEE_SWERVE * Mathf.Sin((_time * FLEE_SWERVE_SPEED) + _orbit.Phase) * dt;
    _velocity -= _velocity * FLEE_DRAG * dt;
    Position += _velocity * dt;

    if (_hasLeftFrame()) {
      Extinguish();
      return;
    }

    var spent = Mathf.InverseLerp(FLEE_FADE_START, FLEE_DURATION, _fleeTime);
    Modulate = new Color(1.0f, 1.0f, 1.0f, 1.0f - Mathf.Clamp(spent, 0.0f, 1.0f));
    if (_fleeTime >= FLEE_DURATION) {
      Extinguish();
    }
  }

  // A bug leans into a climb or a dive and never does anything more than lean. It is drawn the same
  // either way round - the body is an oblong and the wings stand symmetrically over it - so there
  // is no facing to flip and a bug crossing back over itself just leans the other way.
  //
  // The lean comes off the heading doubled, which is what keeps it smooth through vertical: the
  // travel line either side of straight up is very nearly the same line, and anything that read the
  // heading directly would have to jump between leaning hard one way and hard the other to say so.
  // Level at vertical, most leant on the diagonals, and continuous everywhere between.
  private void _face(float dt) {
    var speedSquared = _velocity.LengthSquared();
    if (speedSquared < FACING_MIN_SPEED * FACING_MIN_SPEED) {
      return;
    }
    var tilt = MAX_TILT * 2.0f * _velocity.X * _velocity.Y / speedSquared;
    Rotation = Mathf.LerpAngle(Rotation, tilt, 1.0f - Mathf.Exp(-FACING_TURN * dt));
  }

  // Nothing is watching a bug that has left the frame, so it does not need to be faded out - and a
  // fade that starts while it is still on screen is the swarm evaporating rather than leaving.
  private bool _hasLeftFrame() {
    var viewport = GetViewport();
    if (viewport is null) {
      return false;
    }
    var frame = viewport.GetCanvasTransform().AffineInverse() * viewport.GetVisibleRect();
    return !frame.Grow(OFFSCREEN_MARGIN).HasPoint(GlobalPosition);
  }

  private void _flutter(float dt) {
    var fleeing = _state == State.Fleeing;
    // Off the bug's own phase, or sixteen bugs beat as one and the swarm reads as a mechanism.
    var beat = Mathf.Sin((_time * WING_BEAT * (fleeing ? WING_FLEE_BEAT_GAIN : 1.0f)) + _orbit.Phase);

    // The pair opens out from over the body and comes back together at the top of the stroke,
    // shortening as it closes the way a wing does turning edge-on to the viewer. That shortening is
    // most of what sells a beat at this size: the sweep alone reads as a shrug.
    var opened = (beat * 0.5f) + 0.5f;
    var spread = WING_SPREAD_CLOSED + (WING_SPREAD_RANGE * opened);
    _wingLeftNode.Rotation = -MathUtils.PI2 - spread;
    _wingRightNode.Rotation = -MathUtils.PI2 + spread;
    _wingLeftNode.Scale = new Vector2(_wingScale.X * (1.0f - (WING_FOLD * (1.0f - opened))), _wingScale.Y);
    _wingRightNode.Scale = _wingLeftNode.Scale;

    var flare = _state == State.Hovering ? 1.0f : GLOW_FLARE;
    _flare = Mathf.Lerp(_flare, flare, 1.0f - Mathf.Exp(-GLOW_FLARE_RATE * dt));
    var blink = GLOW_BASE + (GLOW_PULSE * Mathf.Sin((_time * GLOW_SPEED) + _orbit.Phase));
    _glowNode.SelfModulate = new Color(_glowColor, blink * _flare);
  }
}
