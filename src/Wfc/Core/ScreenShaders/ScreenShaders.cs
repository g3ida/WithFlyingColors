using System;
using Godot;

public partial class ScreenShaders : CanvasLayer {
  private enum State { DISABLED, TRANSITION_IN, ENABLED, TRANSITION_OUT }

  private State currentState = State.DISABLED;

  private ColorRect darkerShader;
  private ColorRect simpleBlur;
  private AnimationPlayer darkerShaderAnimationPlayer;
  private AnimationPlayer simpleBlurAnimationPlayer;

  public override void _Ready() {
    darkerShader = GetNode<ColorRect>("DarkerShader/ColorRect");
    simpleBlur = GetNode<ColorRect>("SimpleBlur/ColorRect");
    darkerShaderAnimationPlayer = GetNode<AnimationPlayer>("DarkerShader/ColorRect/AnimationPlayer");
    simpleBlurAnimationPlayer = GetNode<AnimationPlayer>("SimpleBlur/ColorRect/AnimationPlayer");

    darkerShader.Visible = false;
    simpleBlur.Visible = false;
    darkerShaderAnimationPlayer.Play("RESET");
    simpleBlurAnimationPlayer.Play("RESET");
  }

  public void ActivatePauseShader() {
    if (currentState == State.DISABLED) {
      darkerShader.Visible = true;
      simpleBlur.Visible = true;
      darkerShaderAnimationPlayer.Play("Blackout");
      simpleBlurAnimationPlayer.Play("Blur");

      darkerShaderAnimationPlayer.Connect(
        AnimationPlayer.SignalName.AnimationFinished,
        new Callable(this, nameof(OnBlackoutAnimationFinished)),
        flags: (uint)ConnectFlags.OneShot
      );
      currentState = State.TRANSITION_IN;
    }
    else if (currentState == State.TRANSITION_OUT) {
      darkerShaderAnimationPlayer.Disconnect(
        AnimationPlayer.SignalName.AnimationFinished,
        new Callable(this, nameof(OnBlackoutAnimationReversedFinished))
      );
      currentState = State.DISABLED;
      ActivatePauseShader();
    }
  }

  public void DisablePauseShader() {
    if (currentState == State.ENABLED) {
      darkerShaderAnimationPlayer.PlayBackwards("Blackout");
      simpleBlurAnimationPlayer.PlayBackwards("Blur");
      darkerShaderAnimationPlayer.Connect(
        AnimationPlayer.SignalName.AnimationFinished,
        new Callable(this, nameof(OnBlackoutAnimationReversedFinished)),
        flags: (uint)ConnectFlags.OneShot
      );
      currentState = State.TRANSITION_OUT;
    }
    else if (currentState == State.TRANSITION_IN) {
      darkerShaderAnimationPlayer.Disconnect(
        AnimationPlayer.SignalName.AnimationFinished,
        new Callable(this, nameof(OnBlackoutAnimationFinished))
      );
      currentState = State.ENABLED;
      DisablePauseShader();
    }
  }

  private void OnBlackoutAnimationReversedFinished(string animationName) {
    darkerShaderAnimationPlayer.Play("RESET");
    simpleBlurAnimationPlayer.Play("RESET");
    darkerShader.Visible = false;
    simpleBlur.Visible = false;
    currentState = State.DISABLED;
  }

  private void OnBlackoutAnimationFinished(string animationName) {
    darkerShader.Visible = true;
    simpleBlur.Visible = true;
    currentState = State.ENABLED;
  }
}
