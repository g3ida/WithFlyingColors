namespace Wfc.Core;

using System;
using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

public partial class ScreenShaders : CanvasLayer {
  private enum State { Disabled, TransitionIn, Enabled, TransitionOut }

  private State currentState = State.Disabled;

  [NodePath("DarkerShader/ColorRect")]
  private ColorRect _darkerShaderNode = default!;
  [NodePath("SimpleBlur/ColorRect")]
  private ColorRect _simpleBlurNode = default!;
  [NodePath("DarkerShader/ColorRect/AnimationPlayer")]
  private AnimationPlayer _darkerShaderAnimationPlayerNode = default!;
  [NodePath("SimpleBlur/ColorRect/AnimationPlayer")]
  private AnimationPlayer _simpleBlurAnimationPlayerNode = default!;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _darkerShaderNode.Visible = false;
    _simpleBlurNode.Visible = false;
    _darkerShaderAnimationPlayerNode.Play("RESET");
    _simpleBlurAnimationPlayerNode.Play("RESET");
  }

  public void ActivatePauseShader() {
    if (currentState == State.Disabled) {
      _darkerShaderNode.Visible = true;
      _simpleBlurNode.Visible = true;
      _darkerShaderAnimationPlayerNode.Play("Blackout");
      _simpleBlurAnimationPlayerNode.Play("Blur");

      _darkerShaderAnimationPlayerNode.Connect(
        AnimationPlayer.SignalName.AnimationFinished,
        new Callable(this, nameof(OnBlackoutAnimationFinished)),
        flags: (uint)ConnectFlags.OneShot
      );
      currentState = State.TransitionIn;
    }
    else if (currentState == State.TransitionOut) {
      _darkerShaderAnimationPlayerNode.Disconnect(
        AnimationPlayer.SignalName.AnimationFinished,
        new Callable(this, nameof(OnBlackoutAnimationReversedFinished))
      );
      currentState = State.Disabled;
      ActivatePauseShader();
    }
  }

  public void DisablePauseShader() {
    if (currentState == State.Enabled) {
      _darkerShaderAnimationPlayerNode.PlayBackwards("Blackout");
      _simpleBlurAnimationPlayerNode.PlayBackwards("Blur");
      _darkerShaderAnimationPlayerNode.Connect(
        AnimationPlayer.SignalName.AnimationFinished,
        new Callable(this, nameof(OnBlackoutAnimationReversedFinished)),
        flags: (uint)ConnectFlags.OneShot
      );
      currentState = State.TransitionOut;
    }
    else if (currentState == State.TransitionIn) {
      _darkerShaderAnimationPlayerNode.Disconnect(
        AnimationPlayer.SignalName.AnimationFinished,
        new Callable(this, nameof(OnBlackoutAnimationFinished))
      );
      currentState = State.Enabled;
      DisablePauseShader();
    }
  }

  private void OnBlackoutAnimationReversedFinished(string animationName) {
    _darkerShaderAnimationPlayerNode.Play("RESET");
    _simpleBlurAnimationPlayerNode.Play("RESET");
    _darkerShaderNode.Visible = false;
    _simpleBlurNode.Visible = false;
    currentState = State.Disabled;
  }

  private void OnBlackoutAnimationFinished(string animationName) {
    _darkerShaderNode.Visible = true;
    _simpleBlurNode.Visible = true;
    currentState = State.Enabled;
  }
}
