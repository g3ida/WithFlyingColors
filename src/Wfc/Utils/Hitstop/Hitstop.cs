namespace Wfc.Utils;

using Godot;

// Holds the whole engine still for a beat so a hit registers before the motion that follows
// it. Engine.TimeScale is process wide - every timer in the game slows down with it - so this
// keeps the stop short and measures it against the real clock, which is the one thing the
// scale cannot touch.
public partial class Hitstop : Node {
  private ulong _endMsec;
  private bool _isRunning;

  public override void _Ready() {
    base._Ready();
    // The stop has to be able to end itself while the game is paused, or a pause taken during
    // one would come back to a world running at a fraction of speed.
    ProcessMode = ProcessModeEnum.Always;
    SetProcess(false);
  }

  // A stop asked for while one is already running replaces it outright, scale and all. The
  // hit being asked for is the one that just landed, and it is the one the player is waiting
  // to feel.
  public void Start(float duration, float timeScale) {
    if (duration <= 0.0f) {
      return;
    }
    _endMsec = Time.GetTicksMsec() + (ulong)(duration * 1000.0f);
    _isRunning = true;
    Engine.TimeScale = timeScale;
    SetProcess(true);
  }

  public override void _Process(double delta) {
    if (Time.GetTicksMsec() >= _endMsec) {
      _restore();
    }
  }

  public override void _ExitTree() {
    base._ExitTree();
    _restore();
  }

  private void _restore() {
    if (!_isRunning) {
      return;
    }
    _isRunning = false;
    Engine.TimeScale = 1.0;
    SetProcess(false);
  }
}
