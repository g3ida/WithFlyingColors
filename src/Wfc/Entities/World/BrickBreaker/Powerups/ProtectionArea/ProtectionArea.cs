namespace Wfc.Entities.World.BrickBreaker.Powerups;

using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class ProtectionArea : StaticBody2D {
  #region Constants
  private const float SPAWN_FLARE_DURATION = 0.45f;
  private const float SPAWN_FLARE_SPREAD = 2.4f;
  private const float PULSE_PERIOD = 0.9f;
  private const float PULSE_DIM = 0.6f;
  private const float PULSE_BRIGHT = 1.45f;
  #endregion Constants

  #region Nodes
  [NodePath("Glow")]
  private MeshInstance2D _glowNode = default!;
  #endregion Nodes

  private Tween? _pulseTween;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _flareOnSpawn();
    _startPulsing();
  }

  // The shield drops in behind the balls already in play, so it announces itself once by
  // flaring wide before settling to the halo it keeps.
  private void _flareOnSpawn() {
    CreateTween()
      .TweenProperty(_glowNode, "scale:y", 1.0f, SPAWN_FLARE_DURATION)
      .From(SPAWN_FLARE_SPREAD)
      .SetTrans(Tween.TransitionType.Cubic)
      .SetEase(Tween.EaseType.Out);
  }

  private void _startPulsing() {
    var alpha = _glowNode.Modulate.A;
    _pulseTween?.Kill();
    _pulseTween = CreateTween().SetLoops();
    _pulseTween.TweenProperty(_glowNode, "modulate:a", alpha * PULSE_DIM, PULSE_PERIOD)
      .From(alpha * PULSE_BRIGHT)
      .SetTrans(Tween.TransitionType.Sine);
    _pulseTween.TweenProperty(_glowNode, "modulate:a", alpha * PULSE_BRIGHT, PULSE_PERIOD)
      .SetTrans(Tween.TransitionType.Sine);
  }

  private void _onArea2DBodyEntered(Node body) {
    if (body is BouncingBall) {
      QueueFree();
    }
  }
}
