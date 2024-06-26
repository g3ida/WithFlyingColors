using Godot;
using System;

public partial class CameraShake : Node2D
{
    private const Tween.TransitionType TRANS = Tween.TransitionType.Sine;
    private const Tween.EaseType EASE = Tween.EaseType.InOut;

    private Tween tweener;
    private float amplitude;
    private int priority = 0;

    private Camera2D camera = null;

    private Timer DurationNode;
    private Timer FrequencyNode;

    public override void _Ready()
    {
        DurationNode = GetNode<Timer>("Duration");
        FrequencyNode = GetNode<Timer>("Frequency");
        camera = GetParent<Camera2D>();
    }

    public void Start(float duration = 0.15f, float frequency = 10.0f, float _amplitude = 10, int _priority = 0)
    {
        if (_priority >= priority)
        {
            priority = _priority;
            amplitude = _amplitude;
            DurationNode.WaitTime = duration;
            FrequencyNode.WaitTime = 1.0f / frequency;
            DurationNode.Start();
            FrequencyNode.Start();
            NewShake();
        }
    }

    private void CameraTweenInterpolate(Vector2 v)
    {
        tweener?.Kill();
        tweener = CreateTween();
        tweener.TweenProperty(camera, "offset", v, FrequencyNode.WaitTime)
            .SetEase(EASE)
            .SetTrans(TRANS);
    }

    private void NewShake()
    {
        Vector2 rand = new Vector2();
        rand.X = (float)GD.RandRange(-amplitude, amplitude);
        rand.Y = (float)GD.RandRange(-amplitude, amplitude);
        CameraTweenInterpolate(rand);
    }

    private void FinishShake()
    {
        CameraTweenInterpolate(Vector2.Zero);
        priority = 0;
    }

    public void _on_Frequency_timeout()
    {
        NewShake();
    }

    public void _on_Duration_timeout()
    {
        FinishShake();
        FrequencyNode.Stop();
    }
}
