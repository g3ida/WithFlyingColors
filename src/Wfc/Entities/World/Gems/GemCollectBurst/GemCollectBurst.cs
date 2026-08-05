namespace Wfc.Entities.World.Gems;

using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// The flare a gem leaves behind at the point it was taken. It stands in the level on its
// own rather than under the gem, which is hidden the moment its pickup animation ends,
// and it clears itself away once the last spark has burned out.
[ScenePath]
public partial class GemCollectBurst : Node2D {
  private const float FLASH_DURATION = 0.34f;
  private const float FLASH_START_SCALE = 0.12f;
  private const float FLASH_END_SCALE = 0.8f;
  private const float FLASH_SPIN = 0.7f;
  // The gem's own color is what the sparks are cut from; the flash at its heart burns
  // most of the way to white.
  private const float FLASH_WHITENESS = 0.6f;
  private const float LIGHT_PEAK_ENERGY = 2.4f;
  private const float LIGHT_RISE_DURATION = 0.05f;
  private const float LIGHT_FALL_DURATION = 0.45f;

  #region Nodes
  [NodePath("Flash")]
  private Sprite2D _flashNode = default!;
  [NodePath("Sparks")]
  private CpuParticles2D _sparksNode = default!;
  [NodePath("Motes")]
  private CpuParticles2D _motesNode = default!;
  [NodePath("Light")]
  private PointLight2D _lightNode = default!;
  [NodePath("Lifetime")]
  private Timer _lifetimeNode = default!;
  #endregion Nodes

  private Color _color = Colors.White;
  private float _intensity = 1.0f;

  // Called before the burst is in the tree, so what it is told is kept until _Ready has
  // the nodes to put it on.
  public void Setup(Color color, float intensity) {
    _color = color;
    _intensity = intensity;
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _lifetimeNode.Timeout += QueueFree;

    _sparksNode.Color = new Color(_color, _intensity);
    _motesNode.Color = new Color(_color, _intensity);
    _lightNode.Color = _color;
    _flashNode.Modulate = new Color(_color.Lerp(Colors.White, FLASH_WHITENESS), _intensity);
    _flashNode.Scale = Vector2.One * FLASH_START_SCALE;

    _playFlash();
    _playLight();
  }

  private void _playFlash() {
    var tween = CreateTween();
    tween.SetParallel(true);
    tween.TweenProperty(_flashNode, "scale", Vector2.One * FLASH_END_SCALE, FLASH_DURATION)
      .SetTrans(Tween.TransitionType.Quart)
      .SetEase(Tween.EaseType.Out);
    tween.TweenProperty(_flashNode, "modulate:a", 0.0f, FLASH_DURATION)
      .SetTrans(Tween.TransitionType.Quad)
      .SetEase(Tween.EaseType.In);
    tween.TweenProperty(_flashNode, "rotation", FLASH_SPIN, FLASH_DURATION);
  }

  private void _playLight() {
    var tween = CreateTween();
    tween.TweenProperty(_lightNode, "energy", LIGHT_PEAK_ENERGY * _intensity, LIGHT_RISE_DURATION);
    tween.TweenProperty(_lightNode, "energy", 0.0f, LIGHT_FALL_DURATION)
      .SetTrans(Tween.TransitionType.Expo)
      .SetEase(Tween.EaseType.Out);
  }
}
