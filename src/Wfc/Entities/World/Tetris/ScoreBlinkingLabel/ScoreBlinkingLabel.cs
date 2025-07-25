namespace Wfc.Entities.Tetris;

using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class ScoreBlinkingLabel : Label {

  [NodePath("AnimationPlayer")]
  private AnimationPlayer _animationPlayerNode = default!;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
  }

  public void SetValue(string value) {
    Text = value;
    _animationPlayerNode.Play("Blink");
  }
}
