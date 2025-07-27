namespace Wfc.Entities.Ui.UISelect;

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
  [NodePath("HBoxContainer/Right")]
  private Button RightArrowNode = default!;
  [NodePath("HBoxContainer/Label")]
  private Label LabelNode = default!;
  [NodePath("AnimationPlayer")]
  private AnimationPlayer AnimationPlayerNode = default!;
  #endregion Nodes

  private int _index;
  public object? SelectedValue;
  private bool _isReady = false;
  private bool _isInEditMode = false;

  [Signal]
  public delegate void ValueChangedEventHandler(Vector2I value); //FIXME: must work with any type. c# migration.

  [Signal]
  public delegate void SelectionChangedEventHandler(bool is_edit);

  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    _index = SelectDriver.GetDefaultSelectedIndex();
    UpdateSelectedItem();
    UpdateRectSize();
    SetProcess(false);
    _isReady = true;
  }

  public override void _Input(InputEvent @event) {
    if (HasFocus()) {
      if (_isInEditMode) {
        if (Input.IsActionJustPressed("ui_left")) {
          _on_Left_pressed();
          GetViewport().SetInputAsHandled();
        }
        else if (Input.IsActionJustPressed("ui_right")) {
          _on_Right_pressed();
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

  private void _on_Left_pressed() {
    GrabFocus();
    _index = (_index + 1) % (int)SelectDriver.Items.Count;
    UpdateSelectedItem();
    EmitSignal(nameof(ValueChanged), (Vector2I)SelectDriver.ItemValues[_index]);
  }

  private void _on_Right_pressed() {
    GrabFocus();
    _index = (_index - 1 + SelectDriver.Items.Count) % SelectDriver.Items.Count;
    UpdateSelectedItem();
    EmitSignal(nameof(ValueChanged), (Vector2I)SelectDriver.ItemValues[_index]);
  }

  private void UpdateSelectedItem() {
    LabelNode.Text = SelectDriver.Items[_index];
    SelectDriver.onItemSelected(LabelNode.Text);
    SelectedValue = SelectDriver.ItemValues[_index];
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

  private void _on_Label_resized() {
    if (_isReady) {
      UpdateRectSize();
    }
  }
}
