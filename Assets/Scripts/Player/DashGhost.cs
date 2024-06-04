using Godot;

public partial class DashGhost : Sprite2D
{
    private Tween _ghostTween;

    public override void _Ready()
    {
        base._Ready();
        _ghostTween = CreateTween();
        _ghostTween.TweenProperty(this, "modulate:a", 0.0f, 0.25f)
                   .SetTrans(Tween.TransitionType.Quart)
                   .SetEase(Tween.EaseType.Out);
        _ghostTween.Connect("finished", new Callable(this, nameof(OnFinish)), flags: (uint)ConnectFlags.OneShot);

    }

    private void OnFinish()
    {
        _ghostTween.Kill();
        QueueFree();
    }
}
