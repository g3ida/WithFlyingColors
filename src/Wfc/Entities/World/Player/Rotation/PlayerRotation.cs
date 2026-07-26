namespace Wfc.Entities.World.Player;

using System;
using Godot;
using Wfc.Utils;

public partial class PlayerRotation : Node2D {

  [Signal]
  public delegate void RotationCompletedEventHandler();
  private const double DEFAULT_ROTATION_DURATION = 0.1;

  private double _thetaZero; // initial angle, before the rotation is performed.
  private double _thetaTarget; // target angle, after the rotation is completed.
  private double _thetaPoint; // the calculated angle.
  private bool _canRotate = true; // set to false when rotation is in progress.
  private readonly Lazy<CharacterBody2D> _bodyNode;
  private readonly Lazy<Timer> _rotationTimerNode;

  public PlayerRotation() : base() {
    _bodyNode = new Lazy<CharacterBody2D>(GetParent<CharacterBody2D>);
    _rotationTimerNode = new Lazy<Timer>(() => new Timer());
  }

  public PlayerRotation(CharacterBody2D parent) : this() {
    parent.AddChild(this);
    Owner = parent;
  }

  public override void _Ready() {
    base._Ready();
    _setUpTimer();
  }

  private void _setUpTimer() {
    _rotationTimerNode.Value.OneShot = true;
    _rotationTimerNode.Value.Autostart = false;
    // The rotation is integrated in _PhysicsProcess, so the timer that bounds it
    // has to run off the same clock. On Idle (the default) its TimeLeft only
    // moves once per rendered frame, so when rendering is slower than physics
    // the "snap to target" step below never sees TimeLeft drop under a physics
    // delta, and the body keeps turning at full speed past the target.
    _rotationTimerNode.Value.ProcessCallback = Timer.TimerProcessCallback.Physics;
    AddChild(_rotationTimerNode.Value);
    _rotationTimerNode.Value.Timeout += () => {
      _thetaPoint = 0.0f;
      _rotationTimerNode.Value.Stop();
      _canRotate = true;
      EmitSignal(nameof(RotationCompleted));
    };
  }

  public override void _PhysicsProcess(double delta) {
    base._PhysicsProcess(delta);
    if (_rotationTimerNode.Value.TimeLeft > 0) {
      if (_rotationTimerNode.Value.TimeLeft < delta) {
        double currentAngle = _bodyNode.Value.Rotation;
        _thetaPoint = (_thetaTarget - currentAngle) / delta;
      }
      _bodyNode.Value.Rotate((float)(_thetaPoint * delta));
    }
  }

  public bool Fire(
    double angleRadians = Mathf.Pi * 2,
    double duration = DEFAULT_ROTATION_DURATION,
    bool forceRotationIfBusy = true,
    bool cumulateTarget = true,
    bool roundAnglesToNearest = true) {
    if (!_canRotate && !forceRotationIfBusy) {
      return false;
    }

    _canRotate = false;
    _rotationTimerNode.Value.WaitTime = duration;
    _thetaZero = _bodyNode.Value.Rotation;

    if (Mathf.Abs(_thetaPoint) > Mathf.Epsilon && cumulateTarget) {
      _thetaZero = _thetaTarget;
    }

    var unroundedAngle = Mathf.DegToRad(Mathf.RadToDeg(_thetaZero + angleRadians)) / angleRadians;
    if (roundAnglesToNearest) {
      _thetaTarget = Mathf.Round(unroundedAngle) * angleRadians;
    }
    else {
      var roundedAngle = MathUtils.IsPositive(angleRadians) ? Mathf.Ceil(unroundedAngle) : Mathf.Floor(unroundedAngle);
      _thetaTarget = roundedAngle * angleRadians;
    }

    if (Mathf.Abs(_thetaPoint) > Mathf.Epsilon && cumulateTarget) {
      _thetaZero = _bodyNode.Value.Rotation;
    }

    _thetaPoint = (_thetaTarget - _thetaZero) / duration;
    _rotationTimerNode.Value.Start();
    return true;
  }
}
