namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Utils.Attributes;

// One colored speed line. Its tail is pinned to the world at the point the cube passed and its head
// is wherever the cube has actually reached, so the line is only ever as long as the ground the dash
// really covered: one stopped short by a wall stops at the wall with the cube. The sprite's origin
// sits at the tail, which is why stretching it only ever moves the head.
[ScenePath]
public partial class DashStreak : Sprite2D {
  private const float FADE_DURATION = 0.26f;
  private const float FADE_IN_DURATION = 0.04f;
  // The line thins out where it lies rather than retracting, which would drag the head back
  // off the cube it is supposed to be pouring out of.
  private const float SPENT_THICKNESS = 0.3f;

  public Color Tint { get; set; } = Colors.White;
  public float Thickness { get; set; }

  private Player? _cube;
  private Vector2 _forward = Vector2.Right;
  private float _lengthAtFullScale;
  private float _thicknessScale;
  private Tween? _fadeIn;
  private bool _shown;
  private bool _spent;

  public void Follow(Player cube, Vector2 forward) {
    _cube = cube;
    _forward = forward;
  }

  public override void _Ready() {
    base._Ready();
    var texture = Texture.GetSize();
    _lengthAtFullScale = texture.X;
    _thicknessScale = Thickness / texture.Y;

    SelfModulate = Tint;
    Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);
    Scale = new Vector2(0.0f, _thicknessScale);
  }

  // Drawn from the cube's own position rather than played out over the time the dash was expected
  // to take, which is the only way a line can stop where the cube stopped. On the physics clock
  // because the cube is on it: the renderer interpolates both between ticks, so a head written on
  // the render clock would be lerped against a tick-old tail and drift off the cube it came from.
  public override void _PhysicsProcess(double delta) {
    if (_spent) {
      return;
    }
    if (_cube is null || !IsInstanceValid(_cube)) {
      _spend();
      return;
    }

    // The dash is over when the cube says so - it holds the state for its full duration even
    // when a wall has taken all its speed away. Asked before the stretch, so a cube the
    // checkpoint has already put back cannot drag the line across the level on its way out.
    if (!_cube.IsDashing()) {
      _spend();
      return;
    }

    _stretchToCube();
  }

  private void _stretchToCube() {
    var reached = (_cube!.GlobalPosition - GlobalPosition).Dot(_forward);
    Scale = new Vector2(Mathf.Max(reached, 0.0f) / _lengthAtFullScale, Scale.Y);

    // A line whose tail the cube has not reached yet has nothing to show, and one the cube never
    // reaches never shows at all.
    if (!_shown && reached > 0.0f) {
      _shown = true;
      _fadeIn = CreateTween();
      _fadeIn.TweenProperty(this, "modulate:a", 1.0f, FADE_IN_DURATION);
    }
  }

  private void _spend() {
    _spent = true;
    if (!_shown) {
      QueueFree();
      return;
    }

    _fadeIn?.Kill();
    // The thinning is a transform, so it belongs on the clock the stretch was written on.
    var tween = CreateTween().SetProcessMode(Tween.TweenProcessMode.Physics);
    tween.TweenProperty(this, "modulate:a", 0.0f, FADE_DURATION)
         .SetTrans(Tween.TransitionType.Sine)
         .SetEase(Tween.EaseType.In);
    tween.Parallel().TweenProperty(this, "scale:y", _thicknessScale * SPENT_THICKNESS, FADE_DURATION)
         .SetTrans(Tween.TransitionType.Quart)
         .SetEase(Tween.EaseType.Out);
    tween.Finished += QueueFree;
  }
}
