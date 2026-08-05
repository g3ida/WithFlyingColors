namespace Wfc.Entities.World.Platforms;

using Godot;

// The line a sliding platform runs along: a dashed run between two knobs, one at each end of the
// travel. Authoring a platform is otherwise blind - a distance in the inspector says nothing about
// where the platform ends up, and the only way to find out was to play the level.
//
// Drawn top level, so the track stays put while the platform that owns it runs along it. That also
// makes the node's own space the world's, which is the space the ends are handed over in.
[Tool]
public partial class SlideTrack : Node2D {
  #region Constants
  private const float DASH = 14.0f;
  private const float GAP = 10.0f;
  private const float LINE_WIDTH = 3.0f;
  private const float KNOB_RADIUS = 9.0f;
  private const float KNOB_WIDTH = 3.0f;

  // Drawn faint: the track is there to be read off, not to compete with the platform running on it.
  private const float DASH_ALPHA = 0.3f;
  private const float KNOB_ALPHA = 0.55f;

  // Enough segments that a knob this size reads as a circle rather than a polygon.
  private const int KNOB_SEGMENTS = 24;
  #endregion Constants

  private Vector2 _start;
  private Vector2 _end;

  public Vector2 From => _start;
  public Vector2 To => _end;

  public override void _Ready() {
    base._Ready();
    // Set here as well as in the scene, so a track added to a slider by hand behaves like one that
    // came with it.
    TopLevel = true;
  }

  // Where the platform runs from and to, in world coordinates.
  public void Trace(Vector2 start, Vector2 end) {
    if (_start.IsEqualApprox(start) && _end.IsEqualApprox(end)) {
      return;
    }
    _start = start;
    _end = end;
    QueueRedraw();
  }

  public override void _Draw() {
    var span = _end - _start;
    var length = span.Length();
    if (length <= 0.0f) {
      return;
    }

    var step = span / length;
    var dashes = new Color(1.0f, 1.0f, 1.0f, DASH_ALPHA);
    for (var along = 0.0f; along < length; along += DASH + GAP) {
      var dash = Mathf.Min(DASH, length - along);
      DrawLine(_start + (step * along), _start + (step * (along + dash)), dashes, LINE_WIDTH);
    }

    var knobs = new Color(1.0f, 1.0f, 1.0f, KNOB_ALPHA);
    DrawArc(_start, KNOB_RADIUS, 0.0f, Mathf.Tau, KNOB_SEGMENTS, knobs, KNOB_WIDTH, true);
    DrawArc(_end, KNOB_RADIUS, 0.0f, Mathf.Tau, KNOB_SEGMENTS, knobs, KNOB_WIDTH, true);
  }
}
