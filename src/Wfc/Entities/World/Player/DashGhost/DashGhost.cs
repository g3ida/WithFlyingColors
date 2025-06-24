namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class DashGhost : Sprite2D {
  public override void _Ready() {
    base._Ready();
    var ghostTween = CreateTween();
    ghostTween.TweenProperty(this, "modulate:a", 0.0f, 0.25f)
               .SetTrans(Tween.TransitionType.Quart)
               .SetEase(Tween.EaseType.Out);

    ghostTween.Finished += () => {
      ghostTween.Kill();
      QueueFree();
    };
  }
}
