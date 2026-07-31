namespace Wfc.Entities.World.Enemies;

using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// A beam that patrols back and forth along a straight segment. The offset is a
// vector, so a horizontal sweep, a vertical sweep, or anything between is the
// same node with a different export.
[ScenePath]
public partial class SlidingLazer : LazerBeam {
  [Export]
  public Vector2 SlideOffset { get; set; } = new Vector2(0f, 3f * Constants.WORLD_TO_SCREEN);
  // World units per second, like SlidingPlatform's speed.
  [Export]
  public float Speed { get; set; } = 2.0f;
  // Pause at each end of the sweep.
  [Export]
  public float WaitTime { get; set; } = 0.5f;

  private Vector2 _startPosition;
  private float _cycleTime;

  public override void _Ready() {
    base._Ready();
    _startPosition = Position;
  }

  public override void _PhysicsProcess(double delta) {
    if (!Engine.IsEditorHint()) {
      _slide((float)delta);
    }
    base._PhysicsProcess(delta);
  }

  // wait - slide forth - wait - slide back, computed piecewise from one clock:
  // no tween to build or kill, and the position is a pure function of time in
  // the tree. Runs on the physics clock because the beam's raycasts do.
  private void _slide(float delta) {
    var travelTime = SlideOffset.Length() / (Speed * Constants.WORLD_TO_SCREEN);
    if (travelTime <= 0f) {
      return;
    }
    var cycle = 2f * (WaitTime + travelTime);
    _cycleTime = (_cycleTime + delta) % cycle;

    float progress;
    if (_cycleTime < WaitTime) {
      progress = 0f;
    }
    else if (_cycleTime < WaitTime + travelTime) {
      progress = (_cycleTime - WaitTime) / travelTime;
    }
    else if (_cycleTime < 2f * WaitTime + travelTime) {
      progress = 1f;
    }
    else {
      progress = 1f - ((_cycleTime - (2f * WaitTime + travelTime)) / travelTime);
    }
    Position = _startPosition + (SlideOffset * Mathf.SmoothStep(0f, 1f, progress));
  }
}
