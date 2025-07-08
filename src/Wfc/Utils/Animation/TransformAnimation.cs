namespace Wfc.Utils.Animation;

using System;
using Godot;
using Wfc.Utils.Interpolation;

public partial class TransformAnimation : Node {
  private float _animationDuration;
  private Interpolation _interpolation;
  private float _offsetY;

  private float _timer = 0.0f;
  private bool _started = false;

  public TransformAnimation(float animationDuration, Interpolation interpolation, float offsetY) {
    this._animationDuration = animationDuration;
    this._interpolation = interpolation;
    this._offsetY = offsetY;
  }

  public bool IsDone() {
    return _timer <= 0.0f && _started;
  }

  public bool IsRunning() {
    return _timer > 0.0f;
  }

  public void Start() {
    if (IsRunning())
      return;

    _timer = _animationDuration;
    _started = true;
  }

  public void Step(Node2D node, AnimatedSprite2D sprite, float delta) {
    if (_timer > 0) {
      float sinRot = Mathf.Sin(node.Rotation);
      float cosRot = Mathf.Cos(node.Rotation);
      float normalized = _timer / _animationDuration;
      float mean = 1.0f;
      float i = _interpolation.Apply(0.0f, 1.0f, normalized) - mean;

      sprite.Scale = new Vector2(
          mean + (i * Mathf.Abs(cosRot) - Mathf.Abs(sinRot) * i),
          mean + (i * Mathf.Abs(sinRot) - Mathf.Abs(cosRot) * i)
      );

      _timer -= delta;
      sprite.Offset = new Vector2(
          _offsetY * (1.0f - sprite.Scale.X) * sinRot,
          _offsetY * (1.0f - sprite.Scale.Y) * cosRot
      );
    }
    else {
      Reset(sprite);
    }
  }

  public void Reset(AnimatedSprite2D sprite) {
    _timer = 0.0f;
    _started = false;
    sprite.Scale = new Vector2(1.0f, 1.0f);
    sprite.Offset = new Vector2(0.0f, 0.0f);
  }
}
