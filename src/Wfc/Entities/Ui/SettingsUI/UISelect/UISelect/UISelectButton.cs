namespace Wfc.Entities.Ui.SettingsUI.UISelect;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class UISelectButton : Button, IEditableControl {

  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public IInputManager InputManager => this.DependOn<IInputManager>();
  [Dependency]
  public IEventHandler EventHandler => this.DependOn<IEventHandler>();
  #endregion Dependencies


  public UISelectDriver SelectDriver = default!;

  #region Nodes
  [NodePath("HBoxContainer")]
  private HBoxContainer ChildContainerNode = default!;
  [NodePath("HBoxContainer/Left")]
  private Button LeftArrowNode = default!;
  [NodePath("HBoxContainer/Left/AnimationPlayer")]
  private AnimationPlayer LeftArrowAnimationNode = default!;
  [NodePath("HBoxContainer/Right")]
  private Button RightArrowNode = default!;
  [NodePath("HBoxContainer/Right/AnimationPlayer")]
  private AnimationPlayer RightArrowAnimationNode = default!;
  [NodePath("HBoxContainer/Label")]
  private Label LabelNode = default!;
  [NodePath("HBoxContainer/Label/AnimationPlayer")]
  private AnimationPlayer AnimationPlayerNode = default!;
  #endregion Nodes

  private int _index;
  public Variant? SelectedValue = null;
  private bool _isReady = false;
  private bool _isInEditMode = false;

  [Signal]
  public delegate void ValueChangedEventHandler(Variant value);
  [Signal]
  public delegate void SelectionChangedEventHandler(bool isEdit);

  public override void _EnterTree() {
    base._EnterTree();
    this.ChildEnteredTree += _trySetSelectDriver;
  }

  public override void _ExitTree() {
    base._ExitTree();
    this.ChildEnteredTree -= _trySetSelectDriver;
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    _index = SelectDriver.GetDefaultSelectedIndex();
    UpdateSelectedItem();
    UpdateRectSize();
    SetProcess(false);
    _isReady = true;
  }

  private void _trySetSelectDriver(Node child) {
    if (child is UISelectDriver driver) {
      SelectDriver = driver;
    }
  }

  public override void _Input(InputEvent @event) {
    if (HasFocus()) {

      if (InputManager.IsEventActionJustPressed(IInputManager.Action.UIConfirm, @event)) {
        SetEditMode(!_isInEditMode);
        GetViewport().SetInputAsHandled();
        return;
      }
      else if (InputManager.IsEventActionJustPressed(IInputManager.Action.UICancel, @event) && _isInEditMode) {
        SetEditMode(false);
        GetViewport().SetInputAsHandled();
        return;
      }

      if (_isInEditMode) {
        if (InputManager.IsEventActionJustPressed(IInputManager.Action.UILeft, @event)) {
          _onLeftPressed();
          LeftArrowAnimationNode.Play("triggered");
          GetViewport().SetInputAsHandled();
          return;
        }
        else if (InputManager.IsEventActionJustPressed(IInputManager.Action.UIRight, @event)) {
          _onRightPressed();
          RightArrowAnimationNode.Play("triggered");
          GetViewport().SetInputAsHandled();
          return;
        }
        // In case of up/down navigation, exit edit mode
        else if (InputManager.IsEventActionJustPressed(IInputManager.Action.UIUp, @event)
        || InputManager.IsEventActionJustPressed(IInputManager.Action.UIDown, @event)
        || InputManager.IsEventActionJustPressed(IInputManager.Action.UITabNext, @event)
        || InputManager.IsEventActionJustPressed(IInputManager.Action.UITabPrevious, @event)
        ) {
          SetEditMode(false);
          GetViewport().SetInputAsHandled();
          return;
        }
      }
    }
  }

  private void SetEditMode(bool value) {
    if (_isInEditMode && !value) {
      AnimationPlayerNode.Stop();
      AnimationPlayerNode.Play("RESET");
      _isInEditMode = value;
      EmitSelectionChangedSignal();
    }
    if (!_isInEditMode && value) {
      AnimationPlayerNode.Stop();
      AnimationPlayerNode.Play("Blink");
      _isInEditMode = value;
      EmitSelectionChangedSignal();
    }
  }

  private void _onLeftPressed() {
    GrabFocus();
    _index = (_index + 1) % SelectDriver.Items.Count;
    UpdateSelectedItem();
    EmitSignal(nameof(ValueChanged), SelectDriver.ItemValues[_index]);
  }

  private void _onRightPressed() {
    GrabFocus();
    _index = (_index - 1 + SelectDriver.Items.Count) % SelectDriver.Items.Count;
    UpdateSelectedItem();
    EmitSignal(nameof(ValueChanged), SelectDriver.ItemValues[_index]);
  }

  private void UpdateSelectedItem() {
    LabelNode.Text = SelectDriver.Items[_index];
    SelectedValue = SelectDriver.ItemValues[_index];
    SelectDriver.onItemSelected(SelectedValue);
    UpdateRectSize();
  }

  private void UpdateRectSize() {
    SetDeferred(Button.PropertyName.CustomMinimumSize, ChildContainerNode.Size);
    SetDeferred(Control.PropertyName.Size, ChildContainerNode.Size);
  }

  private void EmitSelectionChangedSignal() {
    EmitSignal(nameof(SelectionChanged), _isInEditMode);
  }

  private void _onButtonMouseEntered() {
    GrabFocus();
  }

  private void _onLabelResized() {
    if (_isReady) {
      UpdateRectSize();
    }
  }

  public bool IsInEditMode() => _isInEditMode;
  public void setEditing(bool isEditing) => SetEditMode(isEditing);
}
