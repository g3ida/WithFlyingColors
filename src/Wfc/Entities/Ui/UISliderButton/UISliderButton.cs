namespace Wfc.Entities.Ui;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Input;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[Meta(typeof(IAutoNode))]
public partial class UISliderButton : Button {
  #region Dependencies
  [Dependency]
  public IInputManager InputManager => this.DependOn<IInputManager>();
  #endregion Dependencies

  #region Signals
  [Signal]
  public delegate void ValueChangedEventHandler(float value);
  [Signal]
  public delegate void SelectionChangedEventHandler(bool isSelected);
  #endregion Signals

  #region  Nodes
  [NodePath("HSlider")]
  private HSlider _sliderNode = default!;
  [NodePath("AnimationPlayer")]
  private AnimationPlayer _animationPlayerNode = default!;
  #endregion Nodes
  private bool _isEditing = false;

  public float Value {
    get => (float)_sliderNode.Value;
    set => _sliderNode.Value = value;
  }
  public override void _Notification(int what) => this.Notify(what);

  public void OnResolved() { }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _sliderNode.FocusMode = FocusModeEnum.None;
    FocusMode = FocusModeEnum.All;
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
          _on_left_pressed();
          GetViewport().SetInputAsHandled();
        }
        else if (InputManager.IsJustPressed(IInputManager.Action.UIRight)) {
          _on_right_pressed();
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

  private void _on_left_pressed() {
    AddValueToSlider(-(float)_sliderNode.Step);
  }

  private void _on_right_pressed() {
    AddValueToSlider((float)_sliderNode.Step);
  }

  private void AddValueToSlider(float value) {
    _sliderNode.Value = Mathf.Clamp((float)_sliderNode.Value + value, (float)_sliderNode.MinValue, (float)_sliderNode.MaxValue);
    EmitValueChangedSignal();
  }

  private void _on_mouse_entered() {
    GrabFocus();
  }

  private void _onHSliderDragEnded(double value) {
    EmitValueChangedSignal();
  }

  private void EmitValueChangedSignal() {
    EmitSignal(nameof(ValueChanged), _sliderNode.Value);
  }

  private void EmitSelectionChangedSignal() {
    EmitSignal(nameof(SelectionChanged), _isEditing);
  }
}
