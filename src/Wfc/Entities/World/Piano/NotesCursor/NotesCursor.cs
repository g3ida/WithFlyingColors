namespace Wfc.Entities.World.Piano;

using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class NotesCursor : Sprite2D {
  private const float SPEED = 10f * Constants.WORLD_TO_SCREEN;

  private Tween? _tweener;

  public override void _Ready() {
    base._Ready();
    GetNode<AnimationPlayer>("AnimationPlayer").Play("Blink");
  }

  public void MoveToPosition(Vector2 destPos) {
    float duration = (Position - destPos).Length() / SPEED;
    _tweener?.Kill();
    _tweener = CreateTween();
    _tweener.TweenProperty(this, new NodePath(Node2D.PropertyName.Position), destPos, duration)
        .From(Position)
        .SetTrans(Tween.TransitionType.Quad)
        .SetEase(Tween.EaseType.InOut);
  }
}
