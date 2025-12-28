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

  #region Signals
  [Signal]
  public delegate void SelectionChangedEventHandler(bool isSelected);
  #endregion Signals

  #region  Nodes
  [NodePath("AnimationPlayer")]
  private AnimationPlayer _animationPlayerNode = default!;
  #endregion Nodes
  private bool _isEditing = false;

  public override void _Notification(int what) => this.Notify(what);

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    FocusMode = FocusModeEnum.All;
    CustomMinimumSize = new Vector2(200, CustomMinimumSize.Y);
    Size = new Vector2(350, Size.Y);
  }

  public void _onResized() {
    Size = new Vector2(350, Size.Y);
  }

  public override void _Input(InputEvent @event) {
    if (HasFocus()) {
      if (InputManager.IsJustPressed(IInputManager.Action.UIConfirm)) {
        SetEditing(!_isEditing);
        GetViewport().SetInputAsHandled();
      }
      else if (InputManager.IsJustPressed(IInputManager.Action.UICancel) && _isEditing) {
        SetEditing(false);
        GetViewport().SetInputAsHandled();
      }
      else if (_isEditing) {
        if (InputManager.IsJustPressed(IInputManager.Action.UILeft)) {
          _onLeftPressed();
          GetViewport().SetInputAsHandled();
        }
        else if (InputManager.IsJustPressed(IInputManager.Action.UIRight)) {
          _onRightPressed();
          GetViewport().SetInputAsHandled();
        }
      }
    }
  }
  private void SetEditing(bool value) {
    if (!_isEditing && value) {
      _animationPlayerNode.Stop();
      _animationPlayerNode.Play("Blink");
      EmitSelectionChangedSignal();
    }
    else if (_isEditing && !value) {
      _animationPlayerNode.Stop();
      _animationPlayerNode.Play("RESET");
      EmitSelectionChangedSignal();
    }
    _isEditing = value;
  }

  private void _onLeftPressed() {
    AddValueToSlider(-this.Step);
  }

  private void _onRightPressed() {
    AddValueToSlider(this.Step);
  }
  private void AddValueToSlider(double value) {
    Value = Mathf.Clamp(this.Value + value, this.MinValue, this.MaxValue);
  }

  private void _onMouseEntered() {
    GrabFocus();
  }
  private void EmitSelectionChangedSignal() {
    EmitSignal(nameof(SelectionChanged), _isEditing);
  }
}
