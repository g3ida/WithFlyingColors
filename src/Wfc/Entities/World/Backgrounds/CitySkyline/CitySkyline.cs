namespace Wfc.Entities.World.Backgrounds;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Skin;

// One depth slice of a night skyline: a row of flat blocks standing on a shared
// baseline with lit windows scattered over them. The row is laid out once at
// ready and only redrawn while a window is mid-fade, so a settled slice costs
// nothing per frame.
//
// The blocks are drawn on the Parallax2D itself rather than on a child: the
// repeat that tiles the row across a level is a property of this canvas item
// and does not reach a child's drawing.
public partial class CitySkyline : Parallax2D {
  // A roof of zero size is a block with a flat top; drawing it is a no-op.
  public readonly record struct Building(Rect2 Body, Rect2 Roof, Color Color);

  public struct Window {
    public Rect2 Rect;
    public Color Color;
    public float Brightness;
    public float Alpha;
    public float Target;
    public float Timer;
  }

  #region Constants
  // A lit window must never be mistaken for one of the four colors the player
  // has to match, so a skin tint keeps only part of its saturation and the rest
  // is pulled toward the sodium glow of a street at night.
  private static readonly Color NIGHT_GLOW = new(0.44f, 0.4f, 0.31f);
  private const float WINDOW_VALUE_MAX = 0.72f;
  private const float NIGHT_BLEND = 0.4f;
  private const float WINDOW_BRIGHTNESS_MIN = 0.55f;
  private const float WINDOW_BRIGHTNESS_MAX = 1f;
  // Most of a block is lit by the same fixtures; a few windows break the tint.
  private const float BUILDING_TINT_CHANCE = 0.82f;
  private const float WINDOW_INSET = 18f;

  private const float ROOF_CHANCE = 0.45f;
  private const float ROOF_WIDTH_MIN = 0.3f;
  private const float ROOF_WIDTH_MAX = 0.65f;
  private const float ROOF_HEIGHT_MIN = 20f;
  private const float ROOF_HEIGHT_MAX = 70f;
  // Adjacent blocks read as separate volumes only if their faces differ.
  private const float SHADE_VARIANCE = 0.22f;

  private const float TOGGLE_JITTER_MIN = 0.5f;
  private const float TOGGLE_JITTER_MAX = 1.8f;
  private const float FADE_DURATION = 1.4f;
  private const float SWITCHING_RATIO = 0.22f;
  #endregion Constants

  #region Exports
  // Zero relays out the row on every visit; tests pin a skyline by setting this.
  [Export] public int Seed { get; set; }
  [Export] public float BaselineY { get; set; } = 1080f;
  [Export] public Color BuildingColor { get; set; } = new(0.078f, 0.09f, 0.106f);
  [Export] public float BuildingWidthMin { get; set; } = 110f;
  [Export] public float BuildingWidthMax { get; set; } = 260f;
  [Export] public float BuildingHeightMin { get; set; } = 280f;
  [Export] public float BuildingHeightMax { get; set; } = 800f;
  [Export] public float GapChance { get; set; } = 0.08f;
  [Export] public float WindowSize { get; set; } = 13f;
  [Export] public float WindowPitch { get; set; } = 32f;
  [Export] public float LitChance { get; set; } = 0.26f;
  [Export] public float WindowAlpha { get; set; } = 0.9f;
  // How much of the skin's saturation a lit window keeps: 0 is grey, 1 is the
  // raw skin color. Push it far and the window stops reading as a city light
  // and starts reading as something the player has to match.
  [Export] public float WindowVibrance { get; set; } = 0.6f;
  // Long enough that a window going out is something the player catches out of
  // the corner of an eye rather than a light show competing with the level.
  [Export] public float ToggleIntervalSec { get; set; } = 2f;
  #endregion Exports

  private readonly RandomNumberGenerator _rng = new();
  private Building[] _buildings = [];
  private Window[] _windows = [];
  private int[] _switchers = [];
  private Color[] _palette = [];

  // The row has to land exactly on the repeat or a seam opens where the layer
  // tiles, so the span is the repeat itself rather than a second export that
  // can drift away from it.
  public float SpanWidth => RepeatSize.X;

  public IReadOnlyList<Building> Buildings => _buildings;
  public IReadOnlyList<Window> Windows => _windows;
  public IReadOnlyList<Color> Palette => _palette;
  public IReadOnlyList<int> Switchers => _switchers;

  public override void _Ready() {
    base._Ready();
    if (Seed == 0) {
      _rng.Randomize();
    }
    else {
      _rng.Seed = (ulong)Seed;
    }
    _palette = _washedPalette();
    _layOutBlocks();
    SetProcess(_switchers.Length > 0);
  }

  private void _layOutBlocks() {
    // Without a repeat to fill or a width to roll there is no row, and the roll
    // below would never reach the span.
    if (SpanWidth <= 0f || BuildingWidthMax <= 0f) {
      return;
    }
    // Widths are rolled first and then scaled to land the row exactly on
    // SpanWidth, so the skyline meets itself where the layer repeats.
    var rolled = new List<float>();
    var total = 0f;
    while (total < SpanWidth) {
      var width = _rng.RandfRange(BuildingWidthMin, BuildingWidthMax);
      rolled.Add(width);
      total += width;
    }
    var fit = SpanWidth / total;

    var buildings = new List<Building>(rolled.Count);
    var windows = new List<Window>();
    var switchers = new List<int>();
    var x = 0f;
    foreach (var roll in rolled) {
      var width = roll * fit;
      if (_rng.Randf() >= GapChance) {
        var height = _rng.RandfRange(BuildingHeightMin, BuildingHeightMax);
        var body = new Rect2(x, BaselineY - height, width, height);
        buildings.Add(new Building(body, _roof(body), _shade(BuildingColor)));
        _lightUp(body, windows, switchers);
      }
      x += width;
    }

    _buildings = [.. buildings];
    _windows = [.. windows];
    _switchers = [.. switchers];
  }

  private Rect2 _roof(Rect2 body) {
    if (_rng.Randf() >= ROOF_CHANCE) {
      return default;
    }
    var width = body.Size.X * _rng.RandfRange(ROOF_WIDTH_MIN, ROOF_WIDTH_MAX);
    var height = _rng.RandfRange(ROOF_HEIGHT_MIN, ROOF_HEIGHT_MAX);
    var offset = _rng.RandfRange(0f, body.Size.X - width);
    return new Rect2(body.Position.X + offset, body.Position.Y - height, width, height);
  }

  private Color _shade(Color color) => Color.FromHsv(
      color.H,
      color.S,
      Mathf.Min(1f, color.V * _rng.RandfRange(1f - SHADE_VARIANCE, 1f + SHADE_VARIANCE)));

  private void _lightUp(Rect2 body, List<Window> windows, List<int> switchers) {
    var columns = _cellCount(body.Size.X - (2 * WINDOW_INSET));
    var rows = _cellCount(body.Size.Y - (2 * WINDOW_INSET));
    if (columns <= 0 || rows <= 0 || _palette.Length == 0) {
      return;
    }
    var gridWidth = ((columns - 1) * WindowPitch) + WindowSize;
    var origin = new Vector2(
        body.Position.X + ((body.Size.X - gridWidth) / 2f),
        body.Position.Y + WINDOW_INSET);
    var houseTint = _palette[_rng.RandiRange(0, _palette.Length - 1)];

    for (var row = 0; row < rows; row++) {
      for (var column = 0; column < columns; column++) {
        if (_rng.Randf() >= LitChance) {
          continue;
        }
        var switches = _rng.Randf() < SWITCHING_RATIO;
        // A switcher may start dark and come on later; the timers are seeded
        // anywhere in the cycle so the whole row does not blink in step.
        var lit = !switches || _rng.Randf() < 0.5f ? 1f : 0f;
        if (switches) {
          switchers.Add(windows.Count);
        }
        windows.Add(new Window {
          Rect = new Rect2(
              origin + new Vector2(column * WindowPitch, row * WindowPitch),
              WindowSize,
              WindowSize),
          Color = _rng.Randf() < BUILDING_TINT_CHANCE
              ? houseTint
              : _palette[_rng.RandiRange(0, _palette.Length - 1)],
          Brightness = _rng.RandfRange(WINDOW_BRIGHTNESS_MIN, WINDOW_BRIGHTNESS_MAX),
          Alpha = lit,
          Target = lit,
          Timer = _rng.RandfRange(0f, ToggleIntervalSec * TOGGLE_JITTER_MAX),
        });
      }
    }
  }

  private int _cellCount(float available) =>
      available < WindowSize ? 0 : Mathf.FloorToInt((available - WindowSize) / WindowPitch) + 1;

  private Color[] _washedPalette() {
    var skin = SkinManager.Instance.CurrentSkin;
    var faces = Enum.GetValues<SkinColor>();
    var palette = new Color[faces.Length];
    for (var i = 0; i < faces.Length; i++) {
      palette[i] = _washOut(skin.GetColor(faces[i], SkinColorIntensity.Light));
    }
    return palette;
  }

  private Color _washOut(Color color) => Color.FromHsv(
      color.H,
      color.S * WindowVibrance,
      Mathf.Min(color.V, WINDOW_VALUE_MAX))
    .Lerp(NIGHT_GLOW, NIGHT_BLEND);

  public override void _Process(double delta) {
    base._Process(delta);
    var dt = (float)delta;
    var fading = false;
    foreach (var index in _switchers) {
      ref var window = ref _windows[index];
      if (window.Alpha != window.Target) {
        window.Alpha = Mathf.MoveToward(window.Alpha, window.Target, dt / FADE_DURATION);
        fading = true;
        continue;
      }
      window.Timer -= dt;
      if (window.Timer <= 0f) {
        window.Target = 1f - window.Target;
        window.Timer = ToggleIntervalSec * _rng.RandfRange(TOGGLE_JITTER_MIN, TOGGLE_JITTER_MAX);
      }
    }
    if (fading) {
      QueueRedraw();
    }
  }

  public override void _Draw() {
    foreach (var building in _buildings) {
      DrawRect(building.Body, building.Color);
      DrawRect(building.Roof, building.Color);
    }
    foreach (var window in _windows) {
      if (window.Alpha <= 0f) {
        continue;
      }
      var color = window.Color;
      color.A = WindowAlpha * window.Brightness * window.Alpha;
      DrawRect(window.Rect, color);
    }
  }
}
