using Godot;

public partial class NextNotePointer : Node2D
{
    private const float SPEED = 20f * Constants.WORLD_TO_SCREEN;
    private Tween tweener;

    public override void _Ready()
    {
        GetNode<AnimationPlayer>("ColorRect/AnimationPlayer").Play("Blink");
    }

    public void MoveToPosition(Vector2 destPos)
    {
        float duration = (Position - destPos).Length() / SPEED;
        tweener?.Kill();
        tweener = CreateTween();
        tweener.TweenProperty(this, new NodePath("position"), destPos, duration)
            .From(Position)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);
    }
}
