namespace Wfc.Entities.World.Backgrounds;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Skin;

// One depth slice of the tetris backdrop: blocks scattered over the dark and drawn as nothing but
// their edges. Every one of them is the same thing under the paint - a rectilinear outline with its
// corners rounded off - so a square, a rounded box, a pill, a ring and an elbow all come out of one
// roll. A square rounded by half its side is a ring; a box rounded by half its short side is a pill.
//
// The shapes are laid out once at ready and drawn once. The slice is a Parallax2D and the drawing
// is on the layer itself rather than on a child, because the repeat that tiles the field across a
// level belongs to this canvas item and does not reach a child's drawing.
//
// They wear the skin's own colors, held well under it. A background shape has to stay something the
// eye files as scenery: the four colors are what the player reads a surface by, and an outline in
// full game color hanging in the air is a platform until proven otherwise.
public partial class BlockOutlineField : Parallax2D {
  // Drift and Spin are how far this one shape wanders from where it was laid; the points themselves
  // never move, so the float costs a transform per shape rather than a rebuilt outline.
  public readonly record struct Outline(
    Vector2[] Points,
    Color[] Colors,
    float Width,
    Vector2 Center,
    Vector2 Drift,
    float Spin,
    float Phase,
    float Rate
  );

  #region Constants
  // However vivid the skin, and whatever Vibrance is set to, a shape stays under these. They are
  // what keeps the backdrop from being read as something to land on, so they are not an author's
  // to raise.
  private const float SATURATION_MAX = 0.62f;
  private const float VALUE_MAX = 0.78f;

  // Enough that a ring reads as round at the size these are drawn.
  private const int CORNER_SEGMENTS = 6;

  // How square a shape may be rolled. Flatter than this and an outline reads as a rule rather than
  // a block.
  private const float ASPECT_MIN = 0.36f;

  // How the corners come out. Most blocks are square-cornered, the way a tetromino is; the rest
  // soften, and the fully rounded end of that is where the rings and pills come from.
  private const float SHARP_SHARE = 0.44f;
  private const float FULLY_ROUND_SHARE = 0.26f;
  private const float PART_ROUND_MIN = 0.22f;
  private const float PART_ROUND_MAX = 0.55f;

  private const float ELBOW_THICKNESS_MIN = 0.34f;
  private const float ELBOW_THICKNESS_MAX = 0.5f;

  // Clear air kept around every shape, so the field reads as scattered blocks rather than a tangle.
  private const float SPACING = 22.0f;
  // A rejected roll costs nothing, and a field that gives up early is a field with a bald patch.
  private const int ATTEMPTS_PER_SHAPE = 24;

  // The float. Across and down run at different rates so a shape traces a slow loop rather than
  // sliding up and down a diagonal, and the turn runs at a third rate again so it never lines up
  // with either. Nothing here is meant to be watched - a shape the eye can follow is a shape the
  // eye is spending on the backdrop.
  private const float CROSS_RATE = 0.73f;
  private const float CROSS_PHASE = 1.7f;
  private const float SPIN_RATE = 0.61f;
  // Roughly half a diagonal, which is what a corner swings by when a shape turns.
  private const float CORNER_REACH = 0.71f;
  #endregion Constants

  #region Exports
  // Zero re-scatters the field on every visit; tests pin a layout by setting this.
  [Export] public int Seed { get; set; }
  [Export] public int ShapeCount { get; set; } = 30;
  [Export] public float SizeMin { get; set; } = 52.0f;
  [Export] public float SizeMax { get; set; } = 160.0f;
  // How far a shape wanders from where it was laid, and how far it turns doing it. Small enough
  // that it is never caught in the act - the field reads as adrift rather than as animated.
  [Export] public float DriftMin { get; set; } = 5.0f;
  [Export] public float DriftMax { get; set; } = 15.0f;
  [Export] public float SpinMax { get; set; } = 0.035f;
  [Export] public float PeriodMin { get; set; } = 5.5f;
  [Export] public float PeriodMax { get; set; } = 13.0f;
  [Export] public float StrokeWidth { get; set; } = 5.0f;
  [Export] public float Alpha { get; set; } = 0.5f;
  // How much of the skin's saturation a shape keeps, up to the ceiling above: 0 is grey, 1 is as
  // much of a game color as scenery is ever allowed to show.
  [Export] public float Vibrance { get; set; } = 0.55f;
  // How often an outline runs from one color into another along its length rather than wearing one.
  [Export] public float GradientChance { get; set; } = 0.4f;
  [Export] public float ElbowChance { get; set; } = 0.18f;
  // Clear air kept at the edges of the span, so a shape is never cut in half where the field tiles.
  [Export] public float Margin { get; set; } = 20.0f;
  #endregion Exports

  private readonly RandomNumberGenerator _rng = new RandomNumberGenerator();
  private Outline[] _outlines = [];
  private Color[] _palette = [];
  private float _elapsed;

  // The field has to land exactly on the repeat or a seam opens where the layer tiles, so the span
  // is the repeat itself rather than a second export that can drift away from it.
  public float SpanWidth => RepeatSize.X;
  public float SpanHeight => RepeatSize.Y;

  // The furthest any part of a shape can end up from where it was laid, the wander and the turn
  // together. Everything that has to leave a shape room - the edges of the tile, and the next shape
  // along - reserves this much of it.
  public float Sway => DriftMax + (SizeMax * CORNER_REACH * SpinMax);

  public IReadOnlyList<Outline> Outlines => _outlines;
  public IReadOnlyList<Color> Palette => _palette;

  public override void _Ready() {
    base._Ready();
    if (Seed == 0) {
      _rng.Randomize();
    }
    else {
      _rng.Seed = (ulong)Seed;
    }
    _palette = _washedPalette();
    _scatter();
    SetProcess(_outlines.Length > 0 && Sway > 0.0f);
  }

  public override void _Process(double delta) {
    base._Process(delta);
    _elapsed += (float)delta;
    QueueRedraw();
  }

  public override void _Draw() {
    foreach (var outline in _outlines) {
      DrawSetTransformMatrix(FloatOf(outline, _elapsed));
      DrawPolylineColors(outline.Points, outline.Colors, outline.Width, antialiased: true);
    }
    DrawSetTransformMatrix(Transform2D.Identity);
  }

  // Where a shape has floated to by a given moment: turned about its own middle, then carried off
  // it. Composed rather than handed to DrawSetTransform, which turns about the layer's origin and
  // would swing a shape clear across the screen.
  public Transform2D FloatOf(Outline outline, float seconds) {
    var wander = new Vector2(
      Mathf.Sin((seconds * outline.Rate) + outline.Phase),
      Mathf.Sin((seconds * outline.Rate * CROSS_RATE) + outline.Phase + CROSS_PHASE)
    ) * outline.Drift;
    var turn = outline.Spin * Mathf.Sin((seconds * outline.Rate * SPIN_RATE) + outline.Phase);

    return new Transform2D(turn, outline.Center + wander) * new Transform2D(0.0f, -outline.Center);
  }

  private void _scatter() {
    var inset = Margin + Sway;
    var room = new Vector2(SpanWidth, SpanHeight) - (Vector2.One * 2.0f * inset);
    if (room.X <= SizeMin || room.Y <= SizeMin || _palette.Length == 0) {
      return;
    }

    var boxes = new List<Rect2>(ShapeCount);
    var outlines = new List<Outline>(ShapeCount);
    for (var attempt = 0; attempt < ShapeCount * ATTEMPTS_PER_SHAPE && outlines.Count < ShapeCount; attempt++) {
      var box = _rollBox(inset, room);
      // Grown by the sway as well as the spacing: two shapes laid clear of each other still meet if
      // they both wander toward the gap.
      if (boxes.Exists(placed => placed.Grow(SPACING + Sway).Intersects(box))) {
        continue;
      }
      boxes.Add(box);
      outlines.Add(_outlineOf(box));
    }
    _outlines = [.. outlines];
  }

  private Rect2 _rollBox(float inset, Vector2 room) {
    var along = _rng.RandfRange(SizeMin, SizeMax);
    var across = along * _rng.RandfRange(ASPECT_MIN, 1.0f);
    var size = _rng.Randf() < 0.5f ? new Vector2(along, across) : new Vector2(across, along);
    size = size.Min(room);
    return new Rect2(
      new Vector2(
        _rng.RandfRange(inset, inset + room.X - size.X),
        _rng.RandfRange(inset, inset + room.Y - size.Y)
      ),
      size
    );
  }

  private Outline _outlineOf(Rect2 box) {
    Vector2[] corners;
    // What the corners have to fit across. For a box that is its short side; for an elbow it is the
    // limb rather than the box, since an elbow rounded by more than its own width stops being an
    // elbow and wanders off as a curve.
    float across;
    if (_rng.Randf() < ElbowChance) {
      across = Mathf.Min(box.Size.X, box.Size.Y)
        * _rng.RandfRange(ELBOW_THICKNESS_MIN, ELBOW_THICKNESS_MAX);
      corners = _elbowCorners(box, across);
    }
    else {
      across = Mathf.Min(box.Size.X, box.Size.Y);
      corners = _boxCorners(box);
    }

    var points = _rounded(corners, _cornerRadius(across));
    return new Outline(
      points,
      _colorsAlong(points.Length),
      StrokeWidth,
      box.GetCenter(),
      new Vector2(_rng.RandfRange(DriftMin, DriftMax), _rng.RandfRange(DriftMin, DriftMax)),
      _rng.RandfRange(-SpinMax, SpinMax),
      _rng.RandfRange(0.0f, Mathf.Tau),
      Mathf.Tau / _rng.RandfRange(PeriodMin, PeriodMax)
    );
  }

  // At half of what the corners have to fit across, the straight runs have vanished and the shape
  // has closed into a pill or a ring.
  private float _cornerRadius(float across) {
    var full = across * 0.5f;
    var roll = _rng.Randf();
    if (roll < SHARP_SHARE) {
      return 0.0f;
    }
    if (roll < SHARP_SHARE + FULLY_ROUND_SHARE) {
      return full;
    }
    return full * _rng.RandfRange(PART_ROUND_MIN, PART_ROUND_MAX);
  }

  private static Vector2[] _boxCorners(Rect2 box) => [
    box.Position,
    new Vector2(box.End.X, box.Position.Y),
    box.End,
    new Vector2(box.Position.X, box.End.Y),
  ];

  // An L filling the box, then flipped into whichever of the four ways round it was rolled.
  private Vector2[] _elbowCorners(Rect2 box, float thickness) {
    var corners = new[] {
      box.Position,
      new Vector2(box.Position.X + thickness, box.Position.Y),
      new Vector2(box.Position.X + thickness, box.End.Y - thickness),
      new Vector2(box.End.X, box.End.Y - thickness),
      box.End,
      new Vector2(box.Position.X, box.End.Y),
    };

    var center = box.GetCenter();
    var flipX = _rng.Randf() < 0.5f;
    var flipY = _rng.Randf() < 0.5f;
    for (var index = 0; index < corners.Length; index++) {
      corners[index] = new Vector2(
        flipX ? (2.0f * center.X) - corners[index].X : corners[index].X,
        flipY ? (2.0f * center.Y) - corners[index].Y : corners[index].Y
      );
    }
    return corners;
  }

  // Every corner of a rectilinear outline is a right angle, so the arc that rounds it is centred one
  // radius in along both edges - which holds for the outer corners and for the notch of an elbow
  // alike, the notch simply sweeping the other way. The reach is capped at half the shorter edge so
  // two corners sharing an edge meet rather than overrun each other.
  private static Vector2[] _rounded(Vector2[] corners, float radius) {
    var points = new List<Vector2>((corners.Length * (CORNER_SEGMENTS + 1)) + 1);
    for (var index = 0; index < corners.Length; index++) {
      var previous = corners[(index + corners.Length - 1) % corners.Length];
      var corner = corners[index];
      var next = corners[(index + 1) % corners.Length];

      var reach = Mathf.Min(
        radius,
        Mathf.Min(previous.DistanceTo(corner), corner.DistanceTo(next)) * 0.5f
      );
      if (reach <= 0.0f) {
        points.Add(corner);
        continue;
      }

      var incoming = (corner - previous).Normalized();
      var outgoing = (next - corner).Normalized();
      var start = corner - (incoming * reach);
      var center = start + (outgoing * reach);
      var from = (start - center).Angle();
      var sweep = Mathf.Wrap(
        (corner + (outgoing * reach) - center).Angle() - from,
        -Mathf.Pi,
        Mathf.Pi
      );

      for (var step = 0; step <= CORNER_SEGMENTS; step++) {
        var angle = from + (sweep * step / CORNER_SEGMENTS);
        points.Add(center + (Vector2.FromAngle(angle) * reach));
      }
    }
    points.Add(points[0]);
    return [.. points];
  }

  private Color[] _colorsAlong(int length) {
    var colors = new Color[length];
    var from = _palette[_rng.RandiRange(0, _palette.Length - 1)];
    var to = _rng.Randf() < GradientChance
      ? _palette[_rng.RandiRange(0, _palette.Length - 1)]
      : from;
    for (var index = 0; index < length; index++) {
      var color = from.Lerp(to, length > 1 ? (float)index / (length - 1) : 0.0f);
      color.A = Alpha;
      colors[index] = color;
    }
    return colors;
  }

  private Color[] _washedPalette() {
    var skin = SkinManager.Instance.CurrentSkin;
    var faces = Enum.GetValues<SkinColor>();
    var palette = new Color[faces.Length];
    for (var index = 0; index < faces.Length; index++) {
      palette[index] = _washOut(skin.GetColor(faces[index], SkinColorIntensity.Light));
    }
    return palette;
  }

  private Color _washOut(Color color) => Color.FromHsv(
    color.H,
    Mathf.Min(color.S * Vibrance, SATURATION_MAX),
    Mathf.Min(color.V, VALUE_MAX)
  );
}
