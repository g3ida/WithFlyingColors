namespace Wfc.Entities.World.Player;

using System;
using Godot;
using Wfc.Utils;

public partial class PlayerRotationAction : GodotObject {
  private const float DEFAULT_ROTATION_DURATION = 0.1f;
  private const float FULL_TURN = Mathf.Pi * 2.0f;

  private CountdownTimer _rotationTimer = new CountdownTimer();
  private float _duration;

  // The cube's angle as this action understands it: accumulated across chained rotations,
  // never folded, and the only value the interpolation reads.
  //
  // Node2D.Rotation is derived from the transform, so it always comes back inside
  // (-pi, pi]. Reading it back mid-chain meant that once a chained target passed pi the
  // start and the target sat on opposite branches of the same angle, and closing what was
  // really a quarter turn was computed as most of a full one. Mashing rotate a few times
  // whipped the cube through ~2.5 turns, and during those frames the four BoxFace areas
  // swept the floor fast enough to report a wrong-color contact and kill the player for no
  // visible reason.
  public float CurrentAngle { get; private set; } = 0.0f;

  // Where the current rotation started, in the same unwrapped space as CurrentAngle.
  public float ThetaZero { get; private set; } = 0.0f;
  private float _thetaTarget = 0.0f; // target angle, after the rotation is completed.
  private float _thetaPoint = 0.0f; // angular speed to apply, in radians per second.
  public bool CanRotate { get; private set; } = true; // set to false when rotation is in progress.
  private CharacterBody2D? _body;

  public void SetBody(CharacterBody2D body) {
    _rotationTimer.Set(DEFAULT_ROTATION_DURATION, false);
    this._body = body;
    Reset(body.Rotation);
  }

  // A respawn snaps the cube to a stored angle without any rotation happening, so the
  // accumulator has to be told about it - otherwise the next rotation interpolates from
  // whichever way the player happened to be facing when they died.
  public void Reset(float angle) {
    _rotationTimer.Stop();
    CanRotate = true;
    CurrentAngle = angle;
    ThetaZero = angle;
    _thetaTarget = angle;
    _thetaPoint = 0.0f;
  }

  public void Step(float delta) {
    if (_rotationTimer.IsRunning()) {
      _rotationTimer.Step(delta);
      // The frame the timer runs out lands exactly on the target rather than overshooting
      // by whatever fraction of a frame was left over.
      var isLastFrame = !_rotationTimer.IsRunning();
      var next = isLastFrame ? _thetaTarget : CurrentAngle + _thetaPoint * delta;
      if (isLastFrame) {
        _rotationTimer.Stop();
      }
      _rotateTo(next);
    }
    else if (!CanRotate) {
      _thetaPoint = 0.0f;
      _rotationTimer.Stop();
      CanRotate = true;
      _foldFullTurns();
    }
  }

  public bool Execute(
    int direction,
    float angleRadians = MathUtils.PI2,
    float _duration = DEFAULT_ROTATION_DURATION,
    bool shouldForce = true,
    bool cumulateTarget = true,
    bool useRound = true
  ) {
    if (!CanRotate && !shouldForce)
      return false;
    CanRotate = false;
    this._duration = _duration;
    _rotationTimer.Set(this._duration, false);

    ThetaZero = CurrentAngle;

    // A rotation still in flight: measure the new quarter turn from the target the cube is
    // already heading for, so mashing the button queues turns instead of restarting one.
    if (Math.Abs(_thetaPoint) > Mathf.Epsilon && cumulateTarget) {
      ThetaZero = _thetaTarget;
    }

    float unroundedAngle = (ThetaZero + direction * angleRadians) / angleRadians;
    if (useRound) {
      _thetaTarget = Mathf.Round(unroundedAngle) * angleRadians;
    }
    else {
      float roundedAngle = direction == -1 ? Mathf.Ceil(unroundedAngle) : Mathf.Floor(unroundedAngle);
      _thetaTarget = roundedAngle * angleRadians;
    }

    // ...but the speed still has to be measured from where the cube actually is, or a
    // chained turn would open by jumping forward to the queued target.
    if (Math.Abs(_thetaPoint) > Mathf.Epsilon && cumulateTarget) {
      ThetaZero = CurrentAngle;
    }

    _thetaPoint = (_thetaTarget - ThetaZero) / this._duration;
    _rotationTimer.Reset();
    return true;
  }

  private void _rotateTo(float angle) {
    _body?.Rotate(angle - CurrentAngle);
    CurrentAngle = angle;
  }

  // Nothing is chaining onto this turn any more, so take the same whole number of turns off
  // every angle at once. They stay consistent with each other and with the body, and the
  // accumulator stops growing for as long as the game is running.
  private void _foldFullTurns() {
    var turns = Mathf.Floor((CurrentAngle + Mathf.Pi) / FULL_TURN) * FULL_TURN;
    if (turns == 0.0f) {
      return;
    }
    CurrentAngle -= turns;
    ThetaZero -= turns;
    _thetaTarget -= turns;
  }
}
