namespace Wfc.Entities.Tetris;

using Godot;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;

[ScenePath]
public partial class LevelUp : Node2D {
  #region Constants
  private const float PUNCH_SCALE = 1.25f;
  private const float PUNCH_IN = 0.18f;
  private const float SETTLE = 0.14f;
  private const float HOLD = 0.5f;
  private const float RISE = 0.65f;
  private const float RISE_DISTANCE = 300.0f;

  // Two rings a beat apart, so the burst reads as a shockwave rather than one hoop.
  private const float RING_MAX_RADIUS = 440.0f;
  private const float RING_MIN_RADIUS = 20.0f;
  private const float RING_WIDTH = 16.0f;
  private const float RING_FADE_CURVE = 0.55f;
  private const float RING_THINNING = 0.75f;
  private const float RING_DURATION = 0.85f;
  // Far enough apart to read as two waves; any closer and they overlap into one fat hoop.
  private const float RING_STAGGER = 0.22f;
  private const int RING_SEGMENTS = 64;

  private const float COLOR_STEP = 0.11f;
  #endregion Constants

  #region Nodes
  [NodePath("Label")]
  private Label _labelNode = default!;
  #endregion Nodes

  // Which level was just reached. Only the colour is taken from it, so the popup belongs to the
  // level it announces instead of being the same shade every time.
  public int Level { get; set; } = 1;

  private float _leadRing;
  private float _trailRing;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _labelNode.PivotOffset = _labelNode.Size * 0.5f;
    _labelNode.AddThemeColorOverride("font_color", Colors.White);

    _playRings();
    _playColorCycle();
    _playPunchAndRise();
  }

  public override void _Draw() {
    _drawRing(_leadRing);
    _drawRing(_trailRing);
  }

  // Fades and thins as it travels, so the ring reads as energy leaving rather than a drawn hoop.
  private void _drawRing(float progress) {
    if (progress <= 0.0f || progress >= 1.0f) {
      return;
    }
    // Both curves are deliberately slow to start: a ring that fades and thins straight off the
    // line is gone before it has travelled far enough to read as a wave.
    var color = _accent() with { A = Mathf.Pow(1.0f - progress, RING_FADE_CURVE) };
    DrawArc(
      Vector2.Zero,
      Mathf.Lerp(RING_MIN_RADIUS, RING_MAX_RADIUS, progress),
      0.0f,
      Mathf.Tau,
      RING_SEGMENTS,
      color,
      RING_WIDTH * (1.0f - (progress * RING_THINNING)),
      antialiased: true
    );
  }

  private void _playRings() {
    _tweenRing(v => _leadRing = v, 0.0f);
    _tweenRing(v => _trailRing = v, RING_STAGGER);
  }

  private void _tweenRing(System.Action<float> set, float delay) {
    var tween = CreateTween();
    tween.TweenMethod(
      Callable.From<float>(v => {
        set(v);
        QueueRedraw();
      }),
      0.0f,
      1.0f,
      RING_DURATION
    ).SetDelay(delay).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
  }

  // The font colour rather than the modulate, which the rise below fades out from underneath it.
  // Each step starts where the last one ended, or every one of them washes back through white.
  private void _playColorCycle() {
    var cycle = CreateTween().SetLoops();
    var from = Colors.White;
    foreach (var group in ColorUtils.COLOR_GROUPS) {
      var to = _colorOf(group);
      cycle.TweenMethod(
        Callable.From<Color>(c => _labelNode.AddThemeColorOverride("font_color", c)),
        from,
        to,
        COLOR_STEP
      );
      from = to;
    }
  }

  private void _playPunchAndRise() {
    var sequence = CreateTween();
    sequence.TweenProperty(_labelNode, "scale", Vector2.One * PUNCH_SCALE, PUNCH_IN)
      .From(Vector2.Zero).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    sequence.TweenProperty(_labelNode, "scale", Vector2.One, SETTLE)
      .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    sequence.TweenInterval(HOLD);
    sequence.TweenProperty(this, "position:y", Position.Y - RISE_DISTANCE, RISE)
      .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
    sequence.Parallel().TweenProperty(this, "modulate:a", 0.0f, RISE);
    sequence.TweenCallback(Callable.From(QueueFree));
  }

  private Color _accent() => _colorOf(
    ColorUtils.COLOR_GROUPS[Mathf.PosMod(Level - 1, ColorUtils.COLOR_GROUPS.Length)]
  );

  private static Color _colorOf(string colorGroup) =>
    SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(colorGroup),
      SkinColorIntensity.Basic
    );
}
