using Godot;

public class DashGhost : Sprite
{
    private SceneTreeTween _ghostTween;

    public override void _Ready()
    {
        base._Ready();
        _ghostTween = CreateTween();
        _ghostTween.TweenProperty(this, "modulate:a", 0.0f, 0.25f)
                   .SetTrans(Tween.TransitionType.Quart)
                   .SetEase(Tween.EaseType.Out);
        _ghostTween.Connect("finished", this, nameof(OnFinish), flags: (uint)ConnectFlags.Oneshot);

    }

    private void OnFinish()
    {
        _ghostTween.Kill();
        QueueFree();
    }
}
