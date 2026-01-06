namespace Wfc.Entities.Ui.SettingsUI.UISelect;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Input;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class UISelectButton : Button {

  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public IInputManager InputManager => this.DependOn<IInputManager>();
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

  [Signal]
  public delegate void ValueChangedEventHandler(Variant value);

  public override void _EnterTree() {
    base._EnterTree();
    this.ChildEnteredTree += _trySetSelectDriver;
    this.FocusEntered += _onFocusEntered;
    this.FocusExited += _onFocusExited;
  }

  public override void _ExitTree() {
    base._ExitTree();
    this.ChildEnteredTree -= _trySetSelectDriver;
    this.FocusEntered -= _onFocusEntered;
    this.FocusExited -= _onFocusExited;
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

  public override void _Process(double delta) {
    if (!HasFocus()) {
      return;
    }

    if (InputManager.IsJustPressed(IInputManager.Action.UILeft)) {
      _onLeftPressed();
      LeftArrowAnimationNode.Play("triggered");
    }
    else if (InputManager.IsJustPressed(IInputManager.Action.UIRight)) {
      _onRightPressed();
      RightArrowAnimationNode.Play("triggered");
    }
  }

  private void _onFocusEntered() {
    SetProcess(true);
    AnimationPlayerNode.Stop();
    AnimationPlayerNode.Play("Blink");
  }

  private void _onFocusExited() {
    SetProcess(false);
    AnimationPlayerNode.Stop();
    AnimationPlayerNode.Play("RESET");
  }

  private void _onLeftPressed() {
    _index = (_index + 1) % SelectDriver.Items.Count;
    UpdateSelectedItem();
    EmitSignal(nameof(ValueChanged), SelectDriver.ItemValues[_index]);
  }

  private void _onRightPressed() {
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

  private void _onButtonMouseEntered() {
    GrabFocus();
  }

  private void _onLabelResized() {
    if (_isReady) {
      UpdateRectSize();
    }
  }
}
