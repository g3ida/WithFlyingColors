namespace Wfc.Utils;

using Godot;

// Use built in timer instead. I was new to godot didn't know it existed :p
public partial class CountdownTimer : GodotObject {
  private float _duration;
  private float _timer;

  public CountdownTimer() { }

  public void Set(float _duration, bool isSet) {
    this._duration = _duration;
    _timer = isSet ? _duration : 0.0f;
  }

  public void Reset() {
    _timer = _duration;
  }

  public bool IsRunning() {
    return _timer > 0;
  }

  public void Step(float delta) {
    _timer = Mathf.Max(_timer - delta, 0);
  }

  public void Stop() {
    _timer = 0;
  }
}
