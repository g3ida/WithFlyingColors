using Godot;

public class NotesCursor : Sprite
{
    private const float SPEED = 10f * Constants.WORLD_TO_SCREEN;

    private SceneTreeTween tweener;

    public override void _Ready()
    {
        GetNode<AnimationPlayer>("AnimationPlayer").Play("Blink");
    }

    public void MoveToPosition(Vector2 destPos)
    {
        float duration = (Position - destPos).Length() / SPEED;
        tweener?.Kill();
        tweener = CreateTween();
        tweener.TweenProperty(this, "position", destPos, duration)
            .From(Position)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);
    }
}
