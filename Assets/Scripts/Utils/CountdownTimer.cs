// Deprecated. use built in timer instead. I was new to godot didn't know it existed :p
using Godot;

public class CountdownTimer : Node
{
    private float duration;
    private float timer;

    public CountdownTimer()
    {
        // FIXME: Remove this once migrated to C#
    }

    public void Set(float _duration, bool isSet)
    {
        duration = _duration;
        timer = isSet ? _duration : 0.0f;
    }

    public void Reset()
    {
        timer = duration;
    }

    public bool IsRunning()
    {
        return timer > 0;
    }

    public void Step(float delta)
    {
        timer = Mathf.Max(timer - delta, 0);
    }

    public void Stop()
    {
        timer = 0;
    }
}
