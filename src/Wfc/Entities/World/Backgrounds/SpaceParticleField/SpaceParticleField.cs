namespace Wfc.Entities.World.Backgrounds;

using System.Collections.Generic;
using Godot;
using Wfc.Skin;

// The near layer of a space backdrop: dots, plus signs, rings and dashes in the
// current skin's four face colors, drifting and twinkling in front of the
// greyscale scenery, with the occasional comet dragging a fading tail across
// the screen.
public partial class SpaceParticleField : Node2D {
  public enum Shape {
    Dot,
    Ring,
    Cross,
    Dash,
  }

  public struct Floater {
    public Vector2 Position;
    public Vector2 Velocity;
    public Shape Shape;
    public Color Color;
    public float Size;
    public float Rotation;
    public float RotationSpeed;
    public float TwinklePhase;
    public float TwinkleSpeed;
    public float BaseAlpha;
  }

  #region Constants
  private const float DOT_RADIUS_MIN = 2.5f;
  private const float DOT_RADIUS_MAX = 7f;
  private const float DOT_GLOW_SCALE = 2.2f;
  private const float DOT_GLOW_ALPHA = 0.14f;
  private const float RING_RADIUS_MIN = 5f;
  private const float RING_RADIUS_MAX = 9f;
  private const float RING_WIDTH_RATIO = 0.42f;
  private const float CROSS_HALF_MIN = 8f;
  private const float CROSS_HALF_MAX = 16f;
  private const float CROSS_THICKNESS_RATIO = 0.18f;
  private const float DASH_HALF_MIN = 8f;
  private const float DASH_HALF_MAX = 13f;
  private const float DASH_THICKNESS = 5f;
  private const float DRIFT_SPEED_MIN = 6f;
  private const float DRIFT_SPEED_MAX = 22f;
  // Crosses stay near-upright plus signs; dashes scatter at any slant.
  private const float CROSS_TILT_MAX = 0.12f;
  private const float DASH_TILT_MAX = 0.7f;
  private const float SPIN_SPEED_MAX = 0.1f;
  private const float TWINKLE_SPEED_MIN = 0.5f;
  private const float TWINKLE_SPEED_MAX = 1.6f;
  private const float TWINKLE_DEPTH = 0.35f;
  private const float BASE_ALPHA_MIN = 0.5f;
  private const float BASE_ALPHA_MAX = 0.95f;
  // Wider than the widest floater, so wrapping never pops a visible shape.
  private const float WRAP_MARGIN = 40f;

  private const int TRAIL_POINTS = 40;
  private const float COMET_SPEED_MIN = 260f;
  private const float COMET_SPEED_MAX = 400f;
  private const float COMET_CURVE_MAX = 0.25f;
  private const float COMET_SPREAD = 0.6f;
  private const float COMET_HEAD_RADIUS = 4.2f;
  private const float COMET_TRAIL_WIDTH = 7f;
  private const float COMET_TRAIL_ALPHA = 0.55f;
  // Comets are born far enough off-screen for the tail to be fully grown (and
  // fully drawable) before the head enters the visible rect.
  private const float COMET_SPAWN_MARGIN = 200f;
  private const float COMET_DESPAWN_MARGIN = 500f;
  private const float FIRST_COMET_DELAY_RATIO = 0.3f;
  #endregion Constants

  #region Exports
  [Export] public int FloaterCount { get; set; } = 36;
  [Export] public int MaxComets { get; set; } = 2;
  [Export] public float CometIntervalSec { get; set; } = 6f;
  // Zero reshuffles the field on every visit; tests pin a layout by setting this.
  [Export] public int Seed { get; set; }
  #endregion Exports

  private sealed class Comet {
    public bool Active;
    public Vector2 Velocity;
    public float Curve;
    public Color Color;
    public int Count;
    public Vector2[] History = new Vector2[TRAIL_POINTS];
    public Vector2[] TrailPolygon = new Vector2[2 * TRAIL_POINTS];
    public Color[] TrailColors = new Color[2 * TRAIL_POINTS];
  }

  private readonly RandomNumberGenerator _rng = new();
  private Floater[] _floaters = [];
  private Comet[] _comets = [];
  private Color[] _palette = [];
  private Rect2 _bounds;
  private float _cometTimer;

  public IReadOnlyList<Floater> Floaters => _floaters;
  public IReadOnlyList<Color> Palette => _palette;

  public int ActiveCometCount {
    get {
      var count = 0;
      foreach (var comet in _comets) {
        if (comet.Active) {
          count++;
        }
      }
      return count;
    }
  }

  public override void _Ready() {
    base._Ready();
    if (Seed == 0) {
      _rng.Randomize();
    }
    else {
      _rng.Seed = (ulong)Seed;
    }
    _bounds = GetViewportRect();
    var skin = SkinManager.Instance.CurrentSkin;
    _palette = [
      skin.GetColor(SkinColor.TopFace, SkinColorIntensity.Basic),
      skin.GetColor(SkinColor.LeftFace, SkinColorIntensity.Basic),
      skin.GetColor(SkinColor.BottomFace, SkinColorIntensity.Basic),
      skin.GetColor(SkinColor.RightFace, SkinColorIntensity.Basic),
      skin.GetColor(SkinColor.TopFace, SkinColorIntensity.Light),
      skin.GetColor(SkinColor.LeftFace, SkinColorIntensity.Light),
      skin.GetColor(SkinColor.BottomFace, SkinColorIntensity.Light),
      skin.GetColor(SkinColor.RightFace, SkinColorIntensity.Light),
    ];

    _floaters = new Floater[FloaterCount];
    for (var i = 0; i < _floaters.Length; i++) {
      _floaters[i] = _spawnFloater();
    }
    _comets = new Comet[MaxComets];
    for (var i = 0; i < _comets.Length; i++) {
      _comets[i] = new Comet();
    }
    _cometTimer = CometIntervalSec * FIRST_COMET_DELAY_RATIO;
  }

  private Floater _spawnFloater() {
    var shape = _rng.Randf() switch {
      < 0.45f => Shape.Dot,
      < 0.65f => Shape.Cross,
      < 0.8f => Shape.Ring,
      _ => Shape.Dash,
    };
    var size = shape switch {
      Shape.Dot => _rng.RandfRange(DOT_RADIUS_MIN, DOT_RADIUS_MAX),
      Shape.Ring => _rng.RandfRange(RING_RADIUS_MIN, RING_RADIUS_MAX),
      Shape.Cross => _rng.RandfRange(CROSS_HALF_MIN, CROSS_HALF_MAX),
      _ => _rng.RandfRange(DASH_HALF_MIN, DASH_HALF_MAX),
    };
    return new Floater {
      Position = new Vector2(
          _rng.RandfRange(0, _bounds.Size.X),
          _rng.RandfRange(0, _bounds.Size.Y)),
      Velocity = Vector2.FromAngle(_rng.RandfRange(0, Mathf.Tau))
          * _rng.RandfRange(DRIFT_SPEED_MIN, DRIFT_SPEED_MAX),
      Shape = shape,
      Color = _palette[_rng.RandiRange(0, _palette.Length - 1)],
      Size = size,
      Rotation = shape switch {
        Shape.Cross => _rng.RandfRange(-CROSS_TILT_MAX, CROSS_TILT_MAX),
        Shape.Dash => _rng.RandfRange(-DASH_TILT_MAX, DASH_TILT_MAX),
        _ => 0f,
      },
      RotationSpeed = _rng.RandfRange(-SPIN_SPEED_MAX, SPIN_SPEED_MAX),
      TwinklePhase = _rng.RandfRange(0, Mathf.Tau),
      TwinkleSpeed = _rng.RandfRange(TWINKLE_SPEED_MIN, TWINKLE_SPEED_MAX),
      BaseAlpha = _rng.RandfRange(BASE_ALPHA_MIN, BASE_ALPHA_MAX),
    };
  }

  public override void _Process(double delta) {
    base._Process(delta);
    var dt = (float)delta;
    for (var i = 0; i < _floaters.Length; i++) {
      ref var floater = ref _floaters[i];
      floater.Position += floater.Velocity * dt;
      floater.Rotation += floater.RotationSpeed * dt;
      floater.TwinklePhase += floater.TwinkleSpeed * dt;
      floater.Position = _wrap(floater.Position);
    }

    _cometTimer -= dt;
    if (_cometTimer <= 0f && _tryLaunchComet()) {
      _cometTimer = CometIntervalSec * _rng.RandfRange(0.6f, 1.5f);
    }
    foreach (var comet in _comets) {
      if (comet.Active) {
        _advanceComet(comet, dt);
      }
    }
    QueueRedraw();
  }

  private Vector2 _wrap(Vector2 position) {
    var span = _bounds.Size + new Vector2(2 * WRAP_MARGIN, 2 * WRAP_MARGIN);
    if (position.X < -WRAP_MARGIN) {
      position.X += span.X;
    }
    else if (position.X > _bounds.Size.X + WRAP_MARGIN) {
      position.X -= span.X;
    }
    if (position.Y < -WRAP_MARGIN) {
      position.Y += span.Y;
    }
    else if (position.Y > _bounds.Size.Y + WRAP_MARGIN) {
      position.Y -= span.Y;
    }
    return position;
  }

  private bool _tryLaunchComet() {
    foreach (var comet in _comets) {
      if (comet.Active) {
        continue;
      }
      var side = _rng.RandiRange(0, 3);
      var alongSide = _rng.Randf();
      var (start, inward) = side switch {
        0 => (new Vector2(alongSide * _bounds.Size.X, -COMET_SPAWN_MARGIN), Vector2.Down),
        1 => (new Vector2(alongSide * _bounds.Size.X, _bounds.Size.Y + COMET_SPAWN_MARGIN), Vector2.Up),
        2 => (new Vector2(-COMET_SPAWN_MARGIN, alongSide * _bounds.Size.Y), Vector2.Right),
        _ => (new Vector2(_bounds.Size.X + COMET_SPAWN_MARGIN, alongSide * _bounds.Size.Y), Vector2.Left),
      };
      comet.Active = true;
      comet.Count = 1;
      comet.History[0] = start;
      comet.Velocity = inward.Rotated(_rng.RandfRange(-COMET_SPREAD, COMET_SPREAD))
          * _rng.RandfRange(COMET_SPEED_MIN, COMET_SPEED_MAX);
      comet.Curve = _rng.RandfRange(-COMET_CURVE_MAX, COMET_CURVE_MAX);
      comet.Color = _palette[_rng.RandiRange(0, _palette.Length - 1)];
      return true;
    }
    return false;
  }

  private void _advanceComet(Comet comet, float dt) {
    comet.Velocity = comet.Velocity.Rotated(comet.Curve * dt);
    var head = comet.History[comet.Count - 1] + comet.Velocity * dt;
    if (comet.Count < TRAIL_POINTS) {
      comet.History[comet.Count] = head;
      comet.Count++;
    }
    else {
      System.Array.Copy(comet.History, 1, comet.History, 0, TRAIL_POINTS - 1);
      comet.History[TRAIL_POINTS - 1] = head;
    }
    var flyZone = _bounds.Grow(COMET_DESPAWN_MARGIN);
    if (!flyZone.HasPoint(head)) {
      comet.Active = false;
    }
  }

  public override void _Draw() {
    foreach (var comet in _comets) {
      if (comet.Active) {
        _drawComet(comet);
      }
    }
    foreach (var floater in _floaters) {
      _drawFloater(floater);
    }
  }

  private void _drawFloater(in Floater floater) {
    var color = floater.Color;
    color.A = floater.BaseAlpha
        * (1f - TWINKLE_DEPTH + TWINKLE_DEPTH * Mathf.Sin(floater.TwinklePhase));
    switch (floater.Shape) {
      case Shape.Dot:
        var glow = color;
        glow.A = color.A * DOT_GLOW_ALPHA;
        DrawCircle(floater.Position, floater.Size * DOT_GLOW_SCALE, glow, antialiased: true);
        DrawCircle(floater.Position, floater.Size, color, antialiased: true);
        break;
      case Shape.Ring:
        DrawCircle(floater.Position, floater.Size, color,
            filled: false, width: floater.Size * RING_WIDTH_RATIO, antialiased: true);
        break;
      case Shape.Cross: {
          DrawSetTransform(floater.Position, floater.Rotation);
          var thickness = floater.Size * CROSS_THICKNESS_RATIO;
          DrawRect(new Rect2(-floater.Size, -thickness / 2, 2 * floater.Size, thickness), color);
          DrawRect(new Rect2(-thickness / 2, -floater.Size, thickness, 2 * floater.Size), color);
          DrawSetTransform(Vector2.Zero);
          break;
        }
      case Shape.Dash:
      default: {
          DrawSetTransform(floater.Position, floater.Rotation);
          DrawRect(new Rect2(-floater.Size, -DASH_THICKNESS / 2, 2 * floater.Size, DASH_THICKNESS), color);
          DrawSetTransform(Vector2.Zero);
          break;
        }
    }
  }

  private void _drawComet(Comet comet) {
    var head = comet.History[comet.Count - 1];
    // The tapered tail only exists once the history is complete, which the
    // off-screen spawn margin guarantees happens before the head is visible.
    if (comet.Count == TRAIL_POINTS) {
      for (var i = 0; i < TRAIL_POINTS; i++) {
        var direction = i < TRAIL_POINTS - 1
            ? comet.History[i + 1] - comet.History[i]
            : comet.Velocity;
        var normal = direction.Orthogonal().Normalized();
        var progress = i / (float)(TRAIL_POINTS - 1);
        var halfWidth = Mathf.Max(0.4f, COMET_TRAIL_WIDTH / 2 * Mathf.Pow(progress, 0.8f));
        comet.TrailPolygon[i] = comet.History[i] + normal * halfWidth;
        comet.TrailPolygon[2 * TRAIL_POINTS - 1 - i] = comet.History[i] - normal * halfWidth;
        var color = comet.Color;
        color.A = COMET_TRAIL_ALPHA * Mathf.Pow(progress, 1.5f);
        comet.TrailColors[i] = color;
        comet.TrailColors[2 * TRAIL_POINTS - 1 - i] = color;
      }
      DrawPolygon(comet.TrailPolygon, comet.TrailColors);
    }
    var glow = comet.Color;
    glow.A = DOT_GLOW_ALPHA;
    DrawCircle(head, COMET_HEAD_RADIUS * DOT_GLOW_SCALE, glow, antialiased: true);
    DrawCircle(head, COMET_HEAD_RADIUS, comet.Color, antialiased: true);
  }
}
