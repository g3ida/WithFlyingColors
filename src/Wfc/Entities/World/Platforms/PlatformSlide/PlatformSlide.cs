namespace Wfc.Entities.World.Platforms;

using Godot;
using Wfc.Autoload;
using Wfc.Core.Event;
using Wfc.Core.Serialization;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

// The run a sliding platform makes, kept out of the nodes that host it: a platform that carries
// its own surface and a slider that drives the body it is parented to are the same movement, the
// same crush, and the same thing to put back when the player respawns.
//
// A run is a straight there-and-back along one axis, timed rather than tweened. The phase and how
// far into it the platform has got say where it stands, which is what lets a checkpoint hand back
// the platform the player died under rather than one somewhere else in its cycle.
public sealed class PlatformSlide {
  // Which way a platform runs. Only these two: a platform is read at a glance as a floor that
  // moves or a wall that moves, and a diagonal one reads as neither.
  public enum SlideAxis {
    Horizontal,
    Vertical,
  }

  // A platform waits, runs out, waits again, and comes back. The order is written into scenes as
  // an integer by OneShotPhase, so members are only ever appended.
  public enum SlidePhase {
    WaitAtStart,
    SlidingForth,
    WaitAtEnd,
    SlidingBack,
  }

  // Which end of its run the platform is standing on when the level starts. The level places the
  // platform where it is meant to be found, and the run is measured from there - away from that
  // spot for a platform that starts at the near end, back towards it for one that starts at the far
  // end. So what an author sees is where the platform stands on the level's first frame either way.
  public enum SlideOrigin {
    Start,
    End,
  }

  // Whether the dashed track a platform runs along is drawn. Authoring always wants it; a level
  // that means the track as a hint to the player turns it on for that platform.
  public enum TrackDisplay {
    Never,
    EditorOnly,
    Always,
  }

  // What a checkpoint remembers. Where the platform stood is not among it: the run is fixed by
  // where the level put the platform, so the phase and the time into it place it exactly - and a
  // respawn before the first checkpoint has the authored state to go back to rather than a
  // position nobody ever recorded.
  public sealed record SaveData(
    SlidePhase Phase = default,
    float Elapsed = 0f,
    bool IsStopped = false,
    bool DelayedStop = false
  );

  #region Constants
  // How much of the way to the target the body closes per tick while landing smoothly. Low enough
  // that an arrival reads as a settle rather than a stop.
  private const float SMOOTHING = 0.075f;
  #endregion Constants

  #region Settings
  public SlideAxis Axis = SlideAxis.Horizontal;

  // How far the platform runs, in the units the body is placed in. Negative runs left or up.
  public float Distance = 256.0f;

  // Which end of that run the level placed the platform on.
  public SlideOrigin StartAt = SlideOrigin.Start;

  // World units per second, the same measure the rest of the game moves things by.
  public float Speed = 3.0f;

  // How long the platform stands still at each end.
  public float WaitTime = 4.0f;

  // Held for this long before the first wait of all, and never again. What staggers a run of
  // platforms that would otherwise set off together: give each one a different delay and they
  // cross rather than moving as a wall.
  public float StartDelay;

  // A platform the level starts with parked, for something else to set going.
  public bool StartsStopped;

  // Stop for good on reaching a phase, rather than running the cycle forever.
  public bool OneShot;
  public SlidePhase OneShotPhase = SlidePhase.SlidingBack;

  public bool SmoothLanding;

  // Whether a checkpoint taken while a stop is pending remembers the stop as still pending. Off,
  // the checkpoint remembers the platform as already parked where the stop was going to leave it.
  public bool RestoreDelayedStop;
  #endregion Settings

  #region State
  private PhysicsBody2D? _body;
  private CollisionShape2D? _bodyShape;
  private Vector2 _start;
  // Where the body was last told to be. The body cannot be asked, so the answer is kept.
  private Vector2 _placed;
  private Vector2 _travel;
  private Vector2 _heading;
  private float _legDuration;
  private float _elapsed;
  private bool _delayedStop;
  private SaveData _checkpoint = new SaveData();
  #endregion State

  public SlidePhase Phase { get; private set; }
  public bool IsStopped { get; private set; }

  // How far the body moved on the last tick, signed along the run. What the gear is turned by.
  public float Travelled { get; private set; }

  public Vector2 Start => _start;
  public Vector2 End => _start + _travel;

  // A platform with nowhere to go, which is what an unset Distance leaves. It parks rather than
  // running a cycle that never moves it.
  public bool IsIdle => _travel == Vector2.Zero || _legDuration <= 0.0f;

  // Nothing left to do on this tick or on any tick until something sets the platform going again,
  // so the host can stop being called at all: a parked platform is most of the life of the door
  // that waits for the arena to be cleared.
  public bool IsResting => IsIdle || (IsStopped && Mathf.IsZeroApprox(Travelled));

  // Takes the body over from where the level placed it. Everything the run is measured from is
  // read here, so nothing has to be hunted for again on a tick.
  public void Begin(PhysicsBody2D body) {
    _bodyShape = _findBodyShape(body);
    Remeasure(body);

    Phase = StartAt == SlideOrigin.Start ? SlidePhase.WaitAtStart : SlidePhase.WaitAtEnd;
    // Time owed before the first wait even starts, so the clock begins short of zero rather than at
    // it. Nothing downstream has to know: a platform standing at either end stands where it stands
    // whatever its clock says, and the elapsed a checkpoint saves carries the debt with it.
    _elapsed = -StartDelay;
    _placed = _target();
    Travelled = 0.0f;
    IsStopped = StartsStopped;
    _delayedStop = false;
    _checkpoint = new SaveData(Phase, _elapsed, IsStopped, false);
  }

  // Re-reads a run whose body has been moved, or whose distance or axis has been changed from the
  // inspector.
  public void Remeasure(PhysicsBody2D body) {
    _body = body;
    var offset = Axis == SlideAxis.Horizontal
      ? new Vector2(Distance, 0.0f)
      : new Vector2(0.0f, Distance);
    // Scaled the way the body is: a slider inside a scene that was sized as a whole is authored in
    // that scene's units, not the screen's.
    _travel = offset * body.GlobalScale;
    // The body sits on the end it starts from, so a run that starts at the far end is measured
    // backwards out of where the level put the platform.
    _start = StartAt == SlideOrigin.Start ? body.GlobalPosition : body.GlobalPosition - _travel;
    _heading = _travel.Normalized();
    var speed = Mathf.Abs(Speed) * Constants.WORLD_TO_SCREEN;
    _legDuration = speed > 0.0f ? _travel.Length() / speed : 0.0f;
  }

  public void Step(double delta) {
    _advance(delta);
    _carryBody();
    _reportCrushedPlayer();
  }

  #region Running
  private void _advance(double delta) {
    if (IsStopped || IsIdle) {
      return;
    }

    _elapsed += (float)delta;
    var span = _isSliding(Phase) ? _legDuration : WaitTime;
    if (_elapsed < span) {
      return;
    }
    // The remainder carries into the next phase, so a leg that ends mid-tick does not cost the
    // platform the rest of that tick.
    _elapsed = span > 0.0f ? _elapsed - span : 0.0f;
    _enter(_after(Phase));
  }

  private void _enter(SlidePhase phase) {
    Phase = phase;
    if (_delayedStop) {
      _delayedStop = false;
      IsStopped = true;
    }
    if (OneShot && Phase == OneShotPhase) {
      _delayedStop = true;
    }
  }

  // Where the body belongs right now.
  private Vector2 _target() => Phase switch {
    SlidePhase.SlidingForth => _start.Lerp(End, _progress()),
    SlidePhase.SlidingBack => End.Lerp(_start, _progress()),
    SlidePhase.WaitAtEnd => End,
    _ => _start,
  };

  private float _progress() => _legDuration > 0.0f ? Mathf.Min(_elapsed / _legDuration, 1.0f) : 1.0f;

  private void _carryBody() {
    if (_body is null) {
      return;
    }
    var target = SmoothLanding ? _body.GlobalPosition.Lerp(_target(), SMOOTHING) : _target();
    // Signed along the run, so the cog turns back the way it came on the return leg. Measured
    // against what the body was last told rather than against where it says it is: a body that
    // syncs to physics reports a move only on the tick after it is given one, so reading it back
    // here has the platform standing still through its whole run.
    Travelled = (target - _placed).Dot(_heading);
    _placed = target;
    if (!Mathf.IsZeroApprox(Travelled)) {
      _body.GlobalPosition = target;
    }
  }

  private static bool _isSliding(SlidePhase phase) =>
    phase is SlidePhase.SlidingForth or SlidePhase.SlidingBack;

  private static SlidePhase _after(SlidePhase phase) => phase switch {
    SlidePhase.WaitAtStart => SlidePhase.SlidingForth,
    SlidePhase.SlidingForth => SlidePhase.WaitAtEnd,
    SlidePhase.WaitAtEnd => SlidePhase.SlidingBack,
    SlidePhase.SlidingBack => SlidePhase.WaitAtStart,
    _ => phase,
  };
  #endregion Running

  #region Stopping
  public void Stop(bool immediately) {
    if (IsStopped) {
      return;
    }
    if (immediately) {
      IsStopped = true;
    }
    else {
      _delayedStop = true;
    }
  }

  public void Resume() => IsStopped = false;
  #endregion Stopping

  #region Checkpoints
  public void OnCheckpointReached() =>
    _checkpoint = _delayedStop && !RestoreDelayedStop
      // The platform was already told to stop at the end of this leg, which is something else in
      // the level deciding the ride is over. A respawn that replayed the leg would carry the
      // player back out of wherever the ride left them.
      ? new SaveData(_after(Phase), 0.0f, true, false)
      : new SaveData(Phase, _elapsed, IsStopped, _delayedStop);

  // Puts the platform back where the checkpoint found it, or where the level authored it if the
  // player has not reached a checkpoint yet.
  public void Reset() {
    Phase = _checkpoint.Phase;
    _elapsed = _checkpoint.Elapsed;
    IsStopped = _checkpoint.IsStopped;
    _delayedStop = _checkpoint.DelayedStop;
    Travelled = 0.0f;
    // A platform is listening for a respawn from the moment it enters the tree, which is before it
    // has taken its body over. There is nothing to place yet - the restored phase places it on its
    // first tick instead.
    if (_body is null) {
      return;
    }
    _placed = _target();
    _body.GlobalPosition = _placed;
    _body.ResetPhysicsInterpolation();
  }

  public string Save(ISerializer serializer) => serializer.Serialize(_checkpoint);

  public void Load(ISerializer serializer, string data) {
    _checkpoint = serializer.Deserialize<SaveData>(data) ?? _checkpoint;
    Reset();
  }
  #endregion Checkpoints

  #region Crushing
  private static CollisionShape2D? _findBodyShape(Node2D body) {
    foreach (var child in body.GetChildren()) {
      if (child is CollisionShape2D shape && shape.Shape is RectangleShape2D) {
        return shape;
      }
    }
    return null;
  }

  // The cube has no rigid body to be shoved aside with, so a platform that arrives at it just
  // keeps going and takes the cube with it - through the floor it was standing on, if that is
  // where the platform is headed. Which is why the cube is asked whether it has anywhere left to
  // go rather than watched to see how far in the platform gets: the answer to the second question
  // is always "not much", right up until the cube is somewhere it should never have been.
  private void _reportCrushedPlayer() {
    if (IsStopped || !_isSliding(Phase) || _body is null || _bodyShape is null) {
      return;
    }
    var player = Global.Instance()?.Player;
    if (player is null || !GodotObject.IsInstanceValid(player) || !player.IsInsideTree() || player.IsDying()) {
      return;
    }

    // Which way the platform is under power. Waiting does not count: the destination it is running
    // to over a wait is the place it already stands.
    var travel = Phase == SlidePhase.SlidingForth ? _heading : -_heading;
    var crusher = _worldBodyRect();
    var half = player.GetCollisionHalfExtents();
    var body = new Rect2(player.GlobalPosition - half, half * 2.0f);
    if (!PlatformCrush.HasArrivedInto(crusher, body, travel)) {
      return;
    }
    if (_hasSomewhereToGo(player, PlatformCrush.EscapeMotion(crusher, body, travel))) {
      return;
    }

    EventHandler.Instance.EmitPlayerDying(
      _body,
      PlatformCrush.ContactPoint(crusher, body, travel),
      EntityType.Crusher
    );
  }

  // Whether the body could still be got out of the platform's way, asked of the body's own shape so
  // that what counts as room is whatever that body would collide with - the level's own floor as
  // readily as a tetromino that happened to land there.
  //
  // The platform is left out of the test. The body is already inside it, and the engine answers an
  // overlap it starts out in with a collision whichever way the test motion points - which would
  // make every arrival its own proof, and a cube being lifted into the arena dies of it.
  private bool _hasSomewhereToGo(CharacterBody2D body, Vector2 escape) {
    var probe = new PhysicsTestMotionParameters2D {
      From = body.GlobalTransform,
      Motion = escape,
      ExcludeBodies = new Godot.Collections.Array<Rid> { _body!.GetRid() },
    };
    return !PhysicsServer2D.BodyTestMotion(body.GetRid(), probe);
  }

  // The moving body's own box in world space. The shape is read rather than remembered: a platform
  // sized from the inspector resizes it, and a stale copy of it kills the wrong player.
  private Rect2 _worldBodyRect() {
    var size = ((RectangleShape2D)_bodyShape!.Shape).Size;
    var scale = _body!.GlobalScale.Abs();
    var centre = _body.GlobalPosition + (_bodyShape.Position * scale);
    return new Rect2(centre - (size * scale * 0.5f), size * scale);
  }
  #endregion Crushing
}
