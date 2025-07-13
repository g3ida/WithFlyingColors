namespace Wfc.Utils;

using Godot;

// Use built in timer instead. I was new to godot didn't know it existed :p
public partial class CountdownTimer : GodotObject {
  private float duration;
  private float timer;


  public CountdownTimer() { }

  public void Set(float _duration, bool isSet) {
    duration = _duration;
    timer = isSet ? _duration : 0.0f;
  }

  public void Reset() {
    timer = duration;
  }

  public bool IsRunning() {
    return timer > 0;
  }

  public void Step(float delta) {
    timer = Mathf.Max(timer - delta, 0);
  }

  public void Stop() {
    timer = 0;
  }
}
