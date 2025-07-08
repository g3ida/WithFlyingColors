namespace Wfc.Utils.Animation;

using System;
using Godot;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class SlideAnimation : Node2D {
  private Node2D _thisNode;
  private Vector2 _destination;
  private bool _notified;
  private string _animationName;

  private float _duration;
  private float _timer;
  private Vector2 _oldPosition;
  private Action? _callback = null;

  public SlideAnimation(string animationName, Node2D @this, Vector2 destination, float duration) {
    _thisNode = @this;
    this._destination = destination;
    this._animationName = animationName;
    _oldPosition = _thisNode.Position;
    this._duration = duration;
    _timer = 0;
  }

  public void Update(float delta) {
    _timer += delta;
    if (_timer > _duration) {
      _timer = _duration;
    }

    var weight = _timer / _duration;
    _thisNode.Position = _thisNode.Position.Lerp(_destination, weight);

    if (!_notified && _thisNode.Position == _destination) {
      _notified = true;
      Notify();
    }
  }

  public void SetOnAnimationEndedCallback(Action callback) {
    _callback = callback;
  }

  private void Notify() {
    _callback?.Invoke();
  }
}
