using Godot;
using System;

public partial class ScaleUpPowerUp : PowerUpScript
{
    private const float TWEEN_TIME = 0.7f;
    private const float SCALE_FACTOR = 1.3f;

    private Tween tweener;

    public override void _ExitTree()
    {
        if (IsStillRelevant())
        {
            Player player = Global.Instance().Player;
            player.Scale = new Vector2(1.0f, 1.0f);
        }
    }

    private void InterpolateSize(Player player, float before, float after, float seconds)
    {
        tweener?.Kill();
        tweener = CreateTween();
        tweener.TweenProperty(
            player,
            "scale",
            new Vector2(after, after),
            seconds
        ).SetTrans(Tween.TransitionType.Linear
        ).SetEase(Tween.EaseType.InOut
        ).From(new Vector2(before,before));
    }

    public override void _Ready()
    {
        SetProcess(false);
        Player player = Global.Instance().Player;
        InterpolateSize(player, player.Scale.X, SCALE_FACTOR, TWEEN_TIME);
    }

    public override bool IsStillRelevant()
    {
        Player player = Global.Instance().Player;
        return Mathf.Abs(player.Scale.X - SCALE_FACTOR) < Constants.EPSILON;
    }
}
