namespace Wfc.Entities.Tetris;

using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class ScoreBlinkingLabel : Label {
  #region Constants
  // How far past its resting size the label snaps on a change, and how long it takes to come
  // back. Short enough to read as a knock rather than a wobble.
  private const float PUNCH_SCALE = 1.18f;
  private const float PUNCH_DURATION = 0.28f;
  #endregion Constants

  [NodePath("AnimationPlayer")]
  private AnimationPlayer _animationPlayerNode = default!;

  private Tween? _punch;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    // Scaling a Control works out from its top-left unless it is told otherwise, which throws
    // the line off to the right every time it changes.
    PivotOffset = Size * 0.5f;
  }

  public void SetValue(string value) {
    Text = value;
    _animationPlayerNode.Play("Blink");

    _punch?.Kill();
    Scale = Vector2.One * PUNCH_SCALE;
    _punch = CreateTween().SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    _punch.TweenProperty(this, "scale", Vector2.One, PUNCH_DURATION);
  }
}
