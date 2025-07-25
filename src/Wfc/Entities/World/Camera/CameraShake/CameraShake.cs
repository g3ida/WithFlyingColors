namespace Wfc.Entities.World.Camera;

using System;
using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class CameraShake : Node2D {
  #region Constants
  private const Tween.TransitionType TRANS = Tween.TransitionType.Sine;
  private const Tween.EaseType EASE = Tween.EaseType.InOut;
  #endregion Constants

  #region Nodes
  [NodePath("Duration")]
  private Timer _durationNode = default!;
  [NodePath("Frequency")]
  private Timer _frequencyNode = default!;
  #endregion Nodes

  private Tween? _tweener = null;
  private float _amplitude = 0f;
  private int _priority = 0;
  private Camera2D _camera = default!;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _camera = GetParent<Camera2D>();
  }

  public void Start(float duration = 0.15f, float frequency = 10.0f, float _amplitude = 10, int _priority = 0) {
    if (_priority >= this._priority) {
      this._priority = _priority;
      this._amplitude = _amplitude;
      _durationNode.WaitTime = duration;
      _frequencyNode.WaitTime = 1.0f / frequency;
      _durationNode.Start();
      _frequencyNode.Start();
      _newShake();
    }
  }

  private void _cameraTweenInterpolate(Vector2 v) {
    _tweener?.Kill();
    _tweener = CreateTween();
    _tweener.TweenProperty(_camera, "offset", v, _frequencyNode.WaitTime)
        .SetEase(EASE)
        .SetTrans(TRANS);
  }

  private void _newShake() {
    Vector2 rand = new Vector2();
    rand.X = (float)GD.RandRange(-_amplitude, _amplitude);
    rand.Y = (float)GD.RandRange(-_amplitude, _amplitude);
    _cameraTweenInterpolate(rand);
  }

  private void _finishShake() {
    _cameraTweenInterpolate(Vector2.Zero);
    _priority = 0;
  }

  private void _onFrequencyTimeout() {
    _newShake();
  }

  private void _onDurationTimeout() {
    _finishShake();
    _frequencyNode.Stop();
  }
}
