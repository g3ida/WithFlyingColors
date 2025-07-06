namespace Wfc.Entities.World.Player;

using System;
using Godot;

public partial class PlayerRotationAction : GodotObject {
  private const float DEFAULT_ROTATION_DURATION = 0.1f;
  private CountdownTimer _rotationTimer = new CountdownTimer();
  private float _duration;
  public float ThetaZero { get; private set; } = 0.0f; // initial angle, before the rotation is performed.
  private float _thetaTarget = 0.0f; // target angle, after the rotation is completed.
  private float _thetaPoint = 0.0f; // the calculated angle.
  public bool CanRotate { get; private set; } = true; // set to false when rotation is in progress.
  private CharacterBody2D? _body;

  public void SetBody(CharacterBody2D body) {
    _rotationTimer.Set(DEFAULT_ROTATION_DURATION, false);
    this._body = body;
  }

  public void Step(float delta) {
    if (_rotationTimer.IsRunning()) {
      _rotationTimer.Step(delta);
      if (!_rotationTimer.IsRunning()) {
        // last frame correction
        float currentAngle = _body?.Rotation ?? 0f;
        _thetaPoint = (_thetaTarget - currentAngle) / delta;
        _rotationTimer.Stop();
      }
      _body?.Rotate(_thetaPoint * delta);
    }
    else if (!CanRotate) {
      _thetaPoint = 0.0f;
      _rotationTimer.Stop();
      CanRotate = true;
    }
  }

  public bool Execute(
    int direction,
    float angleRadians = Constants.PI2,
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

    ThetaZero = _body?.Rotation ?? 0f;

    if (Math.Abs(_thetaPoint) > Mathf.Epsilon && cumulateTarget) {
      ThetaZero = _thetaTarget;
    }

    float unroundedAngle = Mathf.DegToRad(Mathf.RadToDeg(ThetaZero + direction * angleRadians)) / angleRadians;
    if (useRound) {
      _thetaTarget = Mathf.Round(unroundedAngle) * angleRadians;
    }
    else {
      float roundedAngle = direction == -1 ? Mathf.Ceil(unroundedAngle) : Mathf.Floor(unroundedAngle);
      _thetaTarget = roundedAngle * angleRadians;
    }

    if (Math.Abs(_thetaPoint) > Mathf.Epsilon && cumulateTarget) {
      ThetaZero = _body?.Rotation ?? 0f;
    }

    _thetaPoint = (_thetaTarget - ThetaZero) / this._duration;
    _rotationTimer.Reset();
    return true;
  }
}
