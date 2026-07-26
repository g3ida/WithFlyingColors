namespace Wfc.Entities.Ui.SettingsUI.UiSlider;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Input;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[Meta(typeof(IAutoNode))]
public partial class UiSlider : HSlider {
  #region Dependencies
  [Dependency]
  public IInputManager InputManager => this.DependOn<IInputManager>();
  #endregion Dependencies

  #region  Nodes
  [NodePath("AnimationPlayer")]
  private AnimationPlayer _animationPlayerNode = default!;
  #endregion Nodes

  public override void _Notification(int what) => this.Notify(what);

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    FocusMode = FocusModeEnum.All;
    CustomMinimumSize = new Vector2(200, CustomMinimumSize.Y);
    Size = new Vector2(350, Size.Y);
    this.GrabFocusOnHover();
    this.BlinkWhileFocused(_animationPlayerNode);
  }

  public override void _EnterTree() {
    this.FocusEntered += _onFocusEntered;
    this.FocusExited += _onFocusExited;
    base._EnterTree();
  }

  public override void _ExitTree() {
    base._ExitTree();

    this.FocusEntered -= _onFocusEntered;
    this.FocusExited -= _onFocusExited;
  }

  public void _onResized() {
    Size = new Vector2(350, Size.Y);
  }

  public override void _Process(double delta) {
    if (!HasFocus()) {
      return;
    }
    if (InputManager.IsJustPressed(IInputManager.Action.UILeft)) {
      _onLeftPressed();
    }
    else if (InputManager.IsJustPressed(IInputManager.Action.UIRight)) {
      _onRightPressed();
    }
  }

  // Left/right is polled, so processing only needs to run while this slider is the
  // one being pointed at. The blink that goes with it is wired in _Ready.
  private void _onFocusEntered() => SetProcess(true);

  private void _onFocusExited() => SetProcess(false);

  private void _onLeftPressed() {
    AddValueToSlider(-this.Step);
  }

  private void _onRightPressed() {
    AddValueToSlider(this.Step);
  }

  private void AddValueToSlider(double value) {
    Value = Mathf.Clamp(this.Value + value, this.MinValue, this.MaxValue);
  }
}
