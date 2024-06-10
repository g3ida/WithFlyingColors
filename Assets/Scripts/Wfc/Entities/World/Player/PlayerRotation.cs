using System.Diagnostics;
using Godot;
using Wfc.Utils;

namespace Wfc.Entities.World.Player;
public partial class PlayerRotation : Node2D {

  private const double _DEFAULT_ROTATION_DURATION = 0.1;

  public double _thetaZero = 0.0; // initial angle, before the rotation is performed.
  private double _thetaTarget = 0.0; // target angle, after the rotation is completed.
  private double _thetaPoint = 0.0; // the calculated angle.
  public bool _canRotate = true; // set to false when rotation is in progress.
  private CharacterBody2D _bodyNode;
  private Timer _rotationTimerNode;

  public PlayerRotation() : base() { }

  public PlayerRotation(Node parent) : base() {
    parent.AddChild(this);
    Owner = parent;
  }

  public override void _Ready() {
    base._Ready();
    _bodyNode = GetParent<CharacterBody2D>();
    _rotationTimerNode = new Timer();
    _SetUpTimer();

  }

  private void _SetUpTimer() {
    _rotationTimerNode.OneShot = true;
    _rotationTimerNode.Autostart = false;
    AddChild(_rotationTimerNode);
    _rotationTimerNode.Timeout += () => {
      _thetaPoint = 0.0f;
      _rotationTimerNode.Stop();
      _canRotate = true;
    };
  }

  public override void _PhysicsProcess(double delta) {
    base._PhysicsProcess(delta);
    if (_rotationTimerNode.TimeLeft > 0) {
      if (_rotationTimerNode.TimeLeft < delta) {
        double currentAngle = _bodyNode.Rotation;
        _thetaPoint = (_thetaTarget - currentAngle) / delta;
      }
      _bodyNode.Rotate((float)(_thetaPoint * delta));
    }
  }

  public bool Fire(
    double angleRadians = Mathf.Pi * 2,
    double duration = _DEFAULT_ROTATION_DURATION,
    bool forceRotationIfBusy = true,
    bool cumulateTarget = true,
    bool roundAnglesToNearest = true) {
    if (!_canRotate && !forceRotationIfBusy)
      return false;

    _canRotate = false;
    _rotationTimerNode.WaitTime = duration;
    _thetaZero = _bodyNode.Rotation;

    if (Mathf.Abs(_thetaPoint) > Mathf.Epsilon && cumulateTarget) {
      _thetaZero = _thetaTarget;
    }

    double unroundedAngle = Mathf.DegToRad(Mathf.RadToDeg(_thetaZero + angleRadians)) / angleRadians;
    if (roundAnglesToNearest) {
      _thetaTarget = Mathf.Round(unroundedAngle) * angleRadians;
    }
    else {
      double roundedAngle = MathUtils.isPositive(angleRadians) ? Mathf.Ceil(unroundedAngle) : Mathf.Floor(unroundedAngle);
      _thetaTarget = roundedAngle * angleRadians;
    }

    if (Mathf.Abs(_thetaPoint) > Mathf.Epsilon && cumulateTarget) {
      _thetaZero = _bodyNode.Rotation;
    }

    _thetaPoint = (_thetaTarget - _thetaZero) / duration;
    _rotationTimerNode.Start();
    return true;
  }
}