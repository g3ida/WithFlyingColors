namespace Wfc.Entities.World.Platforms;

using Godot;
using Wfc.Core.Serialization;

// The turn a rotating platform makes, kept out of the node that hosts it the way PlatformSlide's
// run is: timed rather than tweened, so the phase and how far into it the platform has got say
// which way round it stands - which is what lets a checkpoint hand back the platform the player
// died on rather than one somewhere else in its cycle.
//
// What is carried is the body's angle rather than its position. A platform turns one leg of Sweep
// degrees at a time and stands still between them, so a surface the player is meant to step onto
// spends part of its cycle holding an angle they can step onto. Whether the next leg carries on
// round or comes back is the mode, and both are the same cycle underneath.
public sealed class PlatformSpin {
  // Round and round the same way, or out along an arc and back. A one-way platform is a hazard to
  // be timed; one that comes back is a surface that is level for part of its cycle and a wall for
  // the rest. The order is written into scenes as an integer, so members are only ever appended.
  public enum SpinMode {
    OneWay,
    BackAndForth,
  }

  // A platform waits, turns out, waits again, and turns back - or, going one way, waits and turns
  // out again. The order is written into scenes as an integer by OneShotPhase, so members are only
  // ever appended.
  public enum SpinPhase {
    WaitAtStart,
    TurningForth,
    WaitAtEnd,
    TurningBack,
  }

  // Which end of its sweep the platform is standing at when the level starts. The level places the
  // platform at the angle it is meant to be found at, and the sweep is measured from there - away
  // from that angle for a platform that starts at the near end, back towards it for one that starts
  // at the far end. So what an author sees is how the platform stands on the level's first frame
  // either way.
  public enum SpinOrigin {
    Start,
    End,
  }

  // What a checkpoint remembers. The angle a leg starts from is among it: a platform going one way
  // leaves that angle a leg further round every time it finishes one, so the phase and the time
  // into it place the platform only against the leg it is on.
  public sealed record SaveData(
    SpinPhase Phase = default,
    float Elapsed = 0f,
    float Base = 0f,
    bool IsStopped = false,
    bool DelayedStop = false
  );

  #region Settings
  public SpinMode Mode = SpinMode.OneWay;

  // Degrees per second, signed: positive turns the way a clock does, negative the other way. It is
  // the sign here that says which way the platform goes out, and which way the arrow points.
  public float Speed = 45.0f;

  // How far the platform turns in one leg, in degrees. A quarter turn at a time is a surface that
  // is level as often as it is on end; a full turn is a platform that never stops anywhere else.
  public float Sweep = 90.0f;

  // Which end of that sweep the level placed the platform at. Only a platform that comes back has
  // two ends to be placed at.
  public SpinOrigin StartAt = SpinOrigin.Start;

  // How long the platform stands still at the end of every leg. Zero turns it without a pause.
  public float WaitTime = 1.0f;

  // Held for this long before it first sets off, and never again. What staggers a run of platforms
  // that would otherwise turn in step.
  public float StartDelay;

  // A platform the level starts with parked, for something else to set going.
  public bool StartsStopped;

  // Stop for good on reaching a phase, rather than running the cycle forever.
  public bool OneShot;
  public SpinPhase OneShotPhase = SpinPhase.TurningBack;

  // Whether a checkpoint taken while a stop is pending remembers the stop as still pending. Off,
  // the checkpoint remembers the platform as already parked where the stop was going to leave it.
  public bool RestoreDelayedStop;
  #endregion Settings

  #region State
  private Node2D? _body;
  // The angle a leg starts from, and the signed arc it covers.
  private float _base;
  private float _arc;
  // The angle the body was last told to stand at. The body cannot be asked, so the answer is kept.
  private float _placed;
  private float _legDuration;
  private float _elapsed;
  private bool _delayedStop;
  private SaveData _checkpoint = new SaveData();
  #endregion State

  public SpinPhase Phase { get; private set; }
  public bool IsStopped { get; private set; }

  // How far the body turned on the last tick, signed. What says the platform is under power.
  public float Turned { get; private set; }

  // Which way the platform is turning, or is about to turn out of the end it is waiting at. What
  // the arrow is pointed by: a platform that has arrived at the far end of its sweep already says
  // it is coming back, rather than waiting for the return leg to start before it admits it.
  public float Heading =>
    Phase is SpinPhase.WaitAtEnd or SpinPhase.TurningBack ? -Mathf.Sign(Speed) : Mathf.Sign(Speed);

  // A platform with nowhere to turn, which is what a zero speed or an unset sweep leaves. It parks
  // rather than running a cycle that never moves it.
  public bool IsIdle => Mathf.IsZeroApprox(_arc) || _legDuration <= 0.0f;

  // Nothing left to do on this tick or on any tick until something sets the platform going again,
  // so the host can stop being ticked at all.
  public bool IsResting => IsIdle || (IsStopped && Mathf.IsZeroApprox(Turned));

  // Takes the body over from the angle the level left it at. Everything the turn is measured from
  // is read here, so nothing has to be worked out again on a tick.
  public void Begin(Node2D body) {
    Remeasure(body);

    // Only a platform that comes back has a far end to be found standing at.
    Phase = Mode == SpinMode.BackAndForth && StartAt == SpinOrigin.End
      ? SpinPhase.WaitAtEnd
      : SpinPhase.WaitAtStart;
    // Time owed before the first phase even starts, so the clock begins short of zero rather than
    // at it. Nothing downstream has to know: the angle a leg has reached is clamped to its own
    // ends, and the elapsed a checkpoint saves carries the debt with it.
    _elapsed = -StartDelay;
    _placed = _target();
    Turned = 0.0f;
    IsStopped = StartsStopped;
    _delayedStop = false;
    _checkpoint = new SaveData(Phase, _elapsed, _base, IsStopped, _delayedStop);
  }

  // Re-reads a turn whose body has been turned, or whose sweep or speed has been changed from the
  // inspector.
  public void Remeasure(Node2D body) {
    _body = body;
    var direction = Speed < 0.0f ? -1.0f : 1.0f;
    _arc = direction * Mathf.DegToRad(Mathf.Abs(Sweep));
    // The body stands at the end it starts from, so a sweep that starts at the far end is measured
    // backwards out of the angle the level left the platform at.
    _base = Mode == SpinMode.BackAndForth && StartAt == SpinOrigin.End
      ? body.GlobalRotation - _arc
      : body.GlobalRotation;
    var speed = Mathf.DegToRad(Mathf.Abs(Speed));
    _legDuration = speed > 0.0f ? Mathf.Abs(_arc) / speed : 0.0f;
  }

  public void Step(double delta) {
    _advance(delta);
    _carryBody();
  }

  #region Running
  private void _advance(double delta) {
    if (IsStopped || IsIdle) {
      return;
    }

    _elapsed += (float)delta;
    // As many phases as the tick has time for, rather than one: a platform given no wait at all
    // would otherwise spend a tick standing in each of them, and what should be an unbroken turn
    // catches once a leg. Bounded by the four there are, so a cycle of empty phases cannot spin.
    for (var phases = 0; phases < 4; phases++) {
      var span = _isTurning(Phase) ? _legDuration : WaitTime;
      if (_elapsed < span) {
        return;
      }
      // The remainder carries into the next phase, so a leg that ends mid-tick does not cost the
      // platform the rest of that tick.
      _elapsed -= Mathf.Max(span, 0.0f);
      // A platform going one way never comes back, so the leg it has just finished is where the
      // next one is measured from. Wrapped, to keep the angle it is asked to stand at from growing
      // for as long as the level is open.
      if (Mode == SpinMode.OneWay && _isTurning(Phase)) {
        _base = Mathf.Wrap(_base + _arc, -Mathf.Pi, Mathf.Pi);
      }
      _enter(_after(Phase));
      if (IsStopped) {
        return;
      }
    }
  }

  private void _enter(SpinPhase phase) {
    Phase = phase;
    if (_delayedStop) {
      _delayedStop = false;
      IsStopped = true;
    }
    if (OneShot && Phase == _stopAfter()) {
      _delayedStop = true;
    }
  }

  // The phase whose end the platform stops for good at. A platform going one way passes through
  // only two of them and its cycle is a single leg, so which one is asked for is not the level
  // author's to answer: one shot of it is that leg.
  private SpinPhase _stopAfter() =>
    Mode == SpinMode.OneWay ? SpinPhase.TurningForth : OneShotPhase;

  // Which way round the body belongs right now.
  private float _target() => Phase switch {
    SpinPhase.TurningForth => _base + (_arc * _progress()),
    SpinPhase.TurningBack => _base + (_arc * (1.0f - _progress())),
    SpinPhase.WaitAtEnd => _base + _arc,
    _ => _base,
  };

  // Clamped at both ends: the delay a platform is held by leaves the clock short of zero, and a leg
  // that runs over the end of a tick leaves it past its span.
  private float _progress() =>
    _legDuration > 0.0f ? Mathf.Clamp(_elapsed / _legDuration, 0.0f, 1.0f) : 1.0f;

  private void _carryBody() {
    if (_body is null) {
      return;
    }
    var target = _target();
    // Measured against what the body was last told rather than against what it says: a body that
    // syncs to physics reports a turn only on the tick after it is given one, so reading it back
    // here has the platform standing still through its whole cycle. Taken as the shorter way round,
    // so the tick a platform going one way passes the half turn on reads as the sliver it moved
    // rather than as a turn all the way back - which is most of a revolution of angular velocity
    // handed to whoever is standing on it.
    Turned = Mathf.AngleDifference(_placed, target);
    _placed = target;
    if (!Mathf.IsZeroApprox(Turned)) {
      _body.GlobalRotation = target;
    }
  }

  private static bool _isTurning(SpinPhase phase) =>
    phase is SpinPhase.TurningForth or SpinPhase.TurningBack;

  private SpinPhase _after(SpinPhase phase) => phase switch {
    SpinPhase.WaitAtStart => SpinPhase.TurningForth,
    // A platform going one way stands at the end of every leg the way one that comes back stands at
    // the ends of its sweep, and then sets off again the same way: the angle it waits at is where
    // the next leg starts, so it is the start of a cycle rather than the end of one.
    SpinPhase.TurningForth => Mode == SpinMode.OneWay ? SpinPhase.WaitAtStart : SpinPhase.WaitAtEnd,
    SpinPhase.WaitAtEnd => SpinPhase.TurningBack,
    SpinPhase.TurningBack => SpinPhase.WaitAtStart,
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
      // the level deciding the ride is over. A respawn that replayed the leg would leave the
      // player's route through it turned back to how it stood before.
      ? new SaveData(_after(Phase), 0.0f, _restingBase(), true, false)
      : new SaveData(Phase, _elapsed, _base, IsStopped, _delayedStop);

  // Where the leg the platform is on leaves it, which for one going one way is a leg further round
  // than it is measured from now.
  private float _restingBase() => Mode == SpinMode.OneWay && _isTurning(Phase)
    ? Mathf.Wrap(_base + _arc, -Mathf.Pi, Mathf.Pi)
    : _base;

  // Puts the platform back the way the checkpoint found it, or the way the level authored it if the
  // player has not reached a checkpoint yet.
  public void Reset() {
    Phase = _checkpoint.Phase;
    _elapsed = _checkpoint.Elapsed;
    _base = _checkpoint.Base;
    IsStopped = _checkpoint.IsStopped;
    _delayedStop = _checkpoint.DelayedStop;
    Turned = 0.0f;
    // A platform is listening for a respawn from the moment it enters the tree, which is before it
    // has taken its body over. There is nothing to turn yet - the restored phase places it on its
    // first tick instead.
    if (_body is null) {
      return;
    }
    _placed = _target();
    _body.GlobalRotation = _placed;
    _body.ResetPhysicsInterpolation();
  }

  public string Save(ISerializer serializer) => serializer.Serialize(_checkpoint);

  public void Load(ISerializer serializer, string data) {
    _checkpoint = serializer.Deserialize<SaveData>(data) ?? _checkpoint;
    Reset();
  }
  #endregion Checkpoints
}
