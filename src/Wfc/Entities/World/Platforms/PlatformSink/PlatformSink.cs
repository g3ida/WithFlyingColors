namespace Wfc.Entities.World.Platforms;

using Godot;
using Wfc.Utils;

// The give of a platform that will not hold the player up for long: it sinks while it is being stood
// on, stops once it has given all the way, and comes back up once it is left alone.
//
// Kept out of the node that hosts it for the same reason the slide is: what the platform does is one
// thing and what it is drawn as is another, so a brick shape that sags underfoot is this same give.
//
// Where the platform sinks to is measured from where the level placed it, and nothing else - the
// depth is what an author sets and the surface is only ever between there and its rest. Which is
// also why a respawn has nothing to remember: the platform belongs at its rest, and that is where a
// player who has just died is not standing.
public sealed class PlatformSink {
  #region Settings
  // How far the platform gives, in the units the body is placed in. Downwards, always: the player is
  // standing on it, and a surface that answered their weight by rising would be lifting them.
  public float Depth = 96.0f;

  // World units per second, the same measure the rest of the game moves things by.
  public float SinkSpeed = 1.5f;

  // How fast it comes back, left at zero to come back exactly as fast as it went down. A step that
  // resets faster than it sags is one the player may come back to; a slower one is a step spent.
  public float RiseSpeed;

  // How long the platform stays down after the player leaves it. What stops a sunk step from being
  // there again by the time the player has turned round.
  public float RiseDelay;
  #endregion Settings

  #region State
  private PhysicsBody2D? _body;
  private Vector2 _rest;
  private float _sunk;
  private float _held;
  private bool _isRidden;
  #endregion State

  // How far down the platform has given so far.
  public float Sunk => _sunk;

  // How far the body moved on the last tick, signed downwards. What the cog is turned by.
  public float Travelled { get; private set; }

  public Vector2 Rest => _rest;
  public Vector2 Bottom => _rest + new Vector2(0.0f, Depth);

  // A platform with nothing on it that has nowhere left to come back from, so the host can stop
  // being ticked at all until something stands on it again - which is most of the life of a step.
  public bool IsResting => !_isRidden && Mathf.IsZeroApprox(_sunk) && Mathf.IsZeroApprox(_held);

  // Takes the body over from where the level placed it, which is the rest the give is measured out
  // of and the place a respawn puts it back to.
  public void Begin(PhysicsBody2D body) {
    Remeasure(body);
    _sunk = 0.0f;
    _held = 0.0f;
    _isRidden = false;
    Travelled = 0.0f;
  }

  // Re-reads a platform that has been moved, or whose depth has been changed from the inspector.
  // Only ever while the platform is being authored: the position it would read while playing is
  // wherever the give has got to, which would leave the platform sinking out of its own level.
  public void Remeasure(PhysicsBody2D body) {
    _body = body;
    _rest = body.GlobalPosition;
  }

  public void Step(double delta, bool ridden) {
    _isRidden = ridden;
    var was = _sunk;

    if (ridden) {
      // Reset rather than counted down from the moment the player steps off, so that stepping back
      // on holds the platform down again instead of shortening the next wait.
      _held = RiseDelay;
      _sunk = Mathf.Min(_sunk + (_step(SinkSpeed, delta)), Depth);
    }
    else if (_held > 0.0f) {
      _held -= (float)delta;
    }
    else {
      _sunk = Mathf.Max(_sunk - _step(RiseSpeed > 0.0f ? RiseSpeed : SinkSpeed, delta), 0.0f);
    }

    Travelled = _sunk - was;
    if (!Mathf.IsZeroApprox(Travelled) && _body is not null) {
      _body.GlobalPosition = _rest + new Vector2(0.0f, _sunk);
    }
  }

  // Puts the platform back where the level authored it. A respawn is the player somewhere else, so
  // there is no give left to be partway through.
  public void Reset() {
    _sunk = 0.0f;
    _held = 0.0f;
    _isRidden = false;
    Travelled = 0.0f;
    if (_body is null) {
      return;
    }
    _body.GlobalPosition = _rest;
    _body.ResetPhysicsInterpolation();
  }

  private static float _step(float speed, double delta) =>
    Mathf.Abs(speed) * Constants.WORLD_TO_SCREEN * (float)delta;
}
