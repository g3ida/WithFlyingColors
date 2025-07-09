namespace Wfc.Entities.Ui;

using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class PauseMenuBtn : Button {
  [NodePath("AnimationPlayer")]
  private AnimationPlayer _animationPlayer = null!;
  private enum State { HIDDEN, HIDING, SHOWN, SHOWING }
  private State currentState = State.HIDDEN;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    Visible = false;
    _animationPlayer.Play("Hidden");
    MouseEntered += GrabFocus;

  }

  public void HideBtn() {
    if (currentState == State.SHOWN) {
      currentState = State.HIDING;
      _animationPlayer.Play("Hide");
      _animationPlayer.AnimationFinished += OnHideAnimationDone;
    }
    else if (currentState == State.SHOWING) {
      _animationPlayer.AnimationFinished -= OnShowAnimationDone;
      currentState = State.SHOWN;
      HideBtn();
    }
  }

  public void ShowBtn() {
    if (currentState == State.HIDDEN) {
      currentState = State.SHOWING;
      Visible = true;
      _animationPlayer.PlayBackwards("Hide");
      _animationPlayer.AnimationFinished += OnShowAnimationDone;
    }
    else if (currentState == State.HIDING) {
      _animationPlayer.AnimationFinished -= OnHideAnimationDone;
      currentState = State.HIDDEN;
      ShowBtn();
    }
  }

  private void OnHideAnimationDone(StringName animation) {
    if (currentState != State.HIDING) {
      _animationPlayer.AnimationFinished -= OnHideAnimationDone;
      Visible = false;
      _animationPlayer.Play("Hidden");
      currentState = State.HIDDEN;
    }
  }

  private void OnShowAnimationDone(StringName animation) {
    if (currentState != State.SHOWING) {
      _animationPlayer.AnimationFinished -= OnShowAnimationDone;
      Visible = true;
      _animationPlayer.Play("Shown");
      currentState = State.SHOWN;
    }
  }

  private void _on_PauseMenuButtons_mouse_entered() {
    GrabFocus();
  }
}
