namespace Wfc.Entities.World.Camera;

using System;
using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

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
  private bool _isSubscribed = false;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _camera = GetParent<Camera2D>();
  }

  public override void _EnterTree() {
    base._EnterTree();
    if (!_isSubscribed) {
      EventHandler.Instance.Events.CheckpointLoaded += _onCheckpointLoaded;
      _isSubscribed = true;
    }
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (_isSubscribed) {
      EventHandler.Instance.Events.CheckpointLoaded -= _onCheckpointLoaded;
      _isSubscribed = false;
    }
  }

  // A respawn can land in the middle of a shake - the burst that killed the cube has barely
  // stopped shaking when the reload comes - and the offset the tween was holding would ride
  // onto the restored camera. Whatever is in flight is called off; the respawn starts still.
  private void _onCheckpointLoaded() {
    // The camera and the timers are all picked up in _Ready, one step after the subscription
    // above: a reload arriving in between has nothing to call off yet.
    if (!IsNodeReady()) {
      return;
    }
    _tweener?.Kill();
    _durationNode.Stop();
    _frequencyNode.Stop();
    _priority = 0;
    _camera.Offset = Vector2.Zero;
  }

  public void Start(float duration = 0.15f, float frequency = 10.0f, float amplitude = 10, int priority = 0) {
    if (priority >= _priority) {
      _priority = priority;
      _amplitude = amplitude;
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
