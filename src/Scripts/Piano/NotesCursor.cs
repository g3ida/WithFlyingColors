using Godot;
using Wfc.Utils;

public partial class NotesCursor : Sprite2D {
  private const float SPEED = 10f * Constants.WORLD_TO_SCREEN;

  private Tween tweener;

  public override void _Ready() {
    GetNode<AnimationPlayer>("AnimationPlayer").Play("Blink");
  }

  public void MoveToPosition(Vector2 destPos) {
    float duration = (Position - destPos).Length() / SPEED;
    tweener?.Kill();
    tweener = CreateTween();
    tweener.TweenProperty(this, "position", destPos, duration)
        .From(Position)
        .SetTrans(Tween.TransitionType.Quad)
        .SetEase(Tween.EaseType.InOut);
  }
}
