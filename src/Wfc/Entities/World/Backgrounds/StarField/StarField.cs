namespace Wfc.Entities.World.Backgrounds;

using System.Collections.Generic;
using Godot;

// Distant greyscale scenery for a space backdrop: pinprick stars and a few
// constellation charts. Like the galaxies, everything is too far away to move,
// so the sky is laid out once at ready and drawn exactly once.
public partial class StarField : Node2D {
  public readonly record struct Star(Vector2 Position, float Radius, float Alpha);

  #region Constants
  private static readonly Color GREY = new(0.72f, 0.74f, 0.78f);
  private const float STAR_RADIUS_MIN = 1.0f;
  private const float STAR_RADIUS_MAX = 2.4f;
  private const float STAR_ALPHA_MIN = 0.2f;
  private const float STAR_ALPHA_MAX = 0.65f;
  private const int CONSTELLATION_STARS_MIN = 4;
  private const int CONSTELLATION_STARS_MAX = 7;
  private const float CONSTELLATION_STEP_MIN = 70f;
  private const float CONSTELLATION_STEP_MAX = 130f;
  private const float CONSTELLATION_TURN_MAX = 1.1f;
  private const float CONSTELLATION_DOT_RADIUS = 2.6f;
  private const float CONSTELLATION_LINE_WIDTH = 1.5f;
  private const float CONSTELLATION_LINE_ALPHA = 0.16f;
  private const float CONSTELLATION_DOT_ALPHA = 0.5f;
  private const float EDGE_MARGIN = 70f;
  #endregion Constants

  #region Exports
  [Export] public int StarCount { get; set; } = 90;
  [Export] public int ConstellationCount { get; set; } = 2;
  // Zero reshuffles the sky on every visit; tests pin a layout by setting this.
  [Export] public int Seed { get; set; }
  #endregion Exports

  private Star[] _stars = [];
  private Vector2[][] _constellations = [];

  public IReadOnlyList<Star> Stars => _stars;
  public IReadOnlyList<Vector2[]> Constellations => _constellations;

  public override void _Ready() {
    base._Ready();
    var rng = new RandomNumberGenerator();
    if (Seed == 0) {
      rng.Randomize();
    }
    else {
      rng.Seed = (ulong)Seed;
    }
    var bounds = GetViewportRect().Size;

    _stars = new Star[StarCount];
    for (var i = 0; i < _stars.Length; i++) {
      _stars[i] = new Star(
        new Vector2(rng.RandfRange(0, bounds.X), rng.RandfRange(0, bounds.Y)),
        rng.RandfRange(STAR_RADIUS_MIN, STAR_RADIUS_MAX),
        rng.RandfRange(STAR_ALPHA_MIN, STAR_ALPHA_MAX));
    }

    _constellations = new Vector2[ConstellationCount][];
    for (var i = 0; i < _constellations.Length; i++) {
      _constellations[i] = _wanderingChart(rng, bounds);
    }
  }

  // A constellation reads as a meandering chain of stars, so walk a heading
  // that bends a little at every step and keep the chain inside the screen.
  private static Vector2[] _wanderingChart(RandomNumberGenerator rng, Vector2 bounds) {
    var points = new Vector2[rng.RandiRange(CONSTELLATION_STARS_MIN, CONSTELLATION_STARS_MAX)];
    var position = new Vector2(
        rng.RandfRange(EDGE_MARGIN, bounds.X - EDGE_MARGIN),
        rng.RandfRange(EDGE_MARGIN, bounds.Y - EDGE_MARGIN));
    var heading = rng.RandfRange(0, Mathf.Tau);
    points[0] = position;
    for (var i = 1; i < points.Length; i++) {
      heading += rng.RandfRange(-CONSTELLATION_TURN_MAX, CONSTELLATION_TURN_MAX);
      position += Vector2.FromAngle(heading) * rng.RandfRange(CONSTELLATION_STEP_MIN, CONSTELLATION_STEP_MAX);
      position = position.Clamp(
          new Vector2(EDGE_MARGIN, EDGE_MARGIN),
          new Vector2(bounds.X - EDGE_MARGIN, bounds.Y - EDGE_MARGIN));
      points[i] = position;
    }
    return points;
  }

  public override void _Draw() {
    foreach (var star in _stars) {
      var color = GREY;
      color.A = star.Alpha;
      DrawCircle(star.Position, star.Radius, color, antialiased: true);
    }
    foreach (var chart in _constellations) {
      var line = GREY;
      line.A = CONSTELLATION_LINE_ALPHA;
      for (var i = 1; i < chart.Length; i++) {
        DrawLine(chart[i - 1], chart[i], line, CONSTELLATION_LINE_WIDTH, antialiased: true);
      }
      var dot = GREY;
      dot.A = CONSTELLATION_DOT_ALPHA;
      foreach (var point in chart) {
        DrawCircle(point, CONSTELLATION_DOT_RADIUS, dot, antialiased: true);
      }
    }
  }
}
