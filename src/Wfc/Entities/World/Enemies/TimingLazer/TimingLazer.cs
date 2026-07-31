namespace Wfc.Entities.World.Enemies;

using Godot;
using Wfc.Utils.Attributes;

// A beam that fires and rests on a fixed rhythm. The tail of every rest is a
// telegraph - a faint line along the coming path - so the player is warned
// before it burns again.
[ScenePath]
public partial class TimingLazer : LazerBeam {
  [Export]
  public float FireDuration { get; set; } = 2.0f;
  [Export]
  public float RestDuration { get; set; } = 2.0f;
  // Carved out of the end of the rest, so it never extends the cycle.
  [Export]
  public float TelegraphDuration { get; set; } = 0.6f;
  // Where in the cycle this instance starts: side-by-side lazers with opposite
  // offsets fire in alternation instead of in lockstep.
  [Export]
  public float PhaseOffset { get; set; }

  private float _cycleTime;

  public override void _Ready() {
    base._Ready();
    if (!Engine.IsEditorHint() && _cycleLength() > 0f) {
      _cycleTime = Mathf.PosMod(PhaseOffset, _cycleLength());
      SetBeamState(_stateAt(_cycleTime));
    }
  }

  public override void _PhysicsProcess(double delta) {
    if (!Engine.IsEditorHint() && _cycleLength() > 0f) {
      _cycleTime = (_cycleTime + (float)delta) % _cycleLength();
      SetBeamState(_stateAt(_cycleTime));
    }
    base._PhysicsProcess(delta);
  }

  private float _cycleLength() => FireDuration + RestDuration;

  private BeamState _stateAt(float time) {
    if (time < FireDuration) {
      return BeamState.On;
    }
    var offUntil = FireDuration + Mathf.Max(RestDuration - TelegraphDuration, 0f);
    return time < offUntil ? BeamState.Off : BeamState.Telegraph;
  }
}
