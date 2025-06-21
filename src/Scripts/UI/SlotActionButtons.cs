using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Persistence;

[Meta(typeof(IAutoNode))]
public partial class SlotActionButtons : HBoxContainer {

  public override void _Notification(int what) => this.Notify(what);
  [Signal]
  public delegate void select_button_pressedEventHandler(int slotIndex);
  [Signal]
  public delegate void clear_button_pressedEventHandler(int slotIndex);

  [Dependency]
  public ISaveManager SaveManager => this.DependOn<ISaveManager>();

  public int SlotIndex { get; set; } = 0;

  private SlotButton _deleteButtonNode;
  private SlotButton _confirmButtonNode;
  private Control _spaceNode;

  public void OnResolved() { }

  public override void _Ready() {
    _deleteButtonNode = GetNode<SlotButton>("DeleteButton");
    _confirmButtonNode = GetNode<SlotButton>("ConfirmButton");
    _spaceNode = new Control();

    GetParent().CallDeferred("add_child", _spaceNode);
    _spaceNode.CallDeferred("set_owner", GetParent());

    UpdateSpaceNode(0);
  }

  public void ShowButton() {
    if (ShouldShowDeleteButton()) {
      _deleteButtonNode.ShowButton();
      _deleteButtonNode.GrabFocus();
    }
    if (ShouldShowSelectButton()) {
      _confirmButtonNode.ShowButton();
      _confirmButtonNode.GrabFocus();
    }
  }

  public void HideButton() {
    _deleteButtonNode.HideButton();
    _confirmButtonNode.HideButton();
  }

  private void UpdateSpaceNode(float buttonsSize) {
    float buttonsMaxWidth = SlotButton.BUTTON_MAX_WIDTH * 2;
    _spaceNode.Size = new Vector2(buttonsMaxWidth - buttonsSize, _spaceNode.Size.Y);
    _spaceNode.CustomMinimumSize = new Vector2(buttonsMaxWidth - buttonsSize, _spaceNode.CustomMinimumSize.Y);
  }

  public override void _Process(double delta) {
    float buttonsSize = _deleteButtonNode.Size.X + _confirmButtonNode.Size.X;
    UpdateSpaceNode(buttonsSize);
  }

  public bool ButtonsHasFocus() {
    return _deleteButtonNode.ButtonHasFocus() || _confirmButtonNode.ButtonHasFocus();
  }

  private void _on_DeleteButton_pressed() {
    EmitSignal(nameof(clear_button_pressed), SlotIndex);
  }

  private bool ShouldShowDeleteButton() {
    return SaveManager.IsSLotFilled(SlotIndex);
  }

  private bool ShouldShowSelectButton() {
    return SaveManager.IsSLotFilled(SlotIndex);
  }

  private void _on_ConfirmButton_pressed() {
    EmitSignal(nameof(select_button_pressed), SlotIndex);
  }
}
