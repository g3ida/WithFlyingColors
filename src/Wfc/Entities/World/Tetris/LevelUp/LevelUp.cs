namespace Wfc.Entities.Tetris;

using System;
using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class LevelUp : Node2D {
  #region Nodes
  [NodePath("AnimationPlayer")]
  private AnimationPlayer _animationNode = default!;
  [NodePath("Label/LabelAnimation")]
  private AnimationPlayer _labelAnimationNode = default!;
  #endregion Nodes
  private int _animationCount = 0;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _animationNode.Play("Scale");
    _labelAnimationNode.Play("Fade");
  }

  private void _onLabelAnimationAnimationFinished(string animName) {
    _OnAnimEnd();
  }

  private void _onAnimationPlayerAnimationFinished(string animName) {
    _OnAnimEnd();
  }

  private void _OnAnimEnd() {
    _animationCount++;
    if (_animationCount == 2) {
      QueueFree();
    }
  }
}
