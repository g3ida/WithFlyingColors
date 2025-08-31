namespace Wfc.Entities.Ui.SettingsUI.UISelect;

using System;
using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class UISelectButton : Button {
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
  public delegate void SelectionChangedEventHandler(bool is_edit);

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
      if (_isInEditMode) {
        if (Input.IsActionJustPressed("ui_left")) {
          _onLeftPressed();
          LeftArrowAnimationNode.Play("triggered");
          GetViewport().SetInputAsHandled();
        }
        else if (Input.IsActionJustPressed("ui_right")) {
          _onRightPressed();
          RightArrowAnimationNode.Play("triggered");
          GetViewport().SetInputAsHandled();
        }
      }

      if (Input.IsActionJustPressed("ui_accept")) {
        SetEditMode(!_isInEditMode);
        GetViewport().SetInputAsHandled();
      }
      else if (Input.IsActionJustPressed("ui_cancel") && _isInEditMode) {
        SetEditMode(false);
        GetViewport().SetInputAsHandled();
      }
    }
  }

  private void SetEditMode(bool value) {
    if (_isInEditMode && !value) {
      AnimationPlayerNode.Stop();
      AnimationPlayerNode.Play("RESET");
      EmitSelectionChangedSignal();
    }
    if (!_isInEditMode && value) {
      AnimationPlayerNode.Stop();
      AnimationPlayerNode.Play("Blink");
      EmitSelectionChangedSignal();
    }
    _isInEditMode = value;
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
}
