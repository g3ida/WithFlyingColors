namespace Wfc.Entities.Ui.Slots;

using System;
using System.ComponentModel.DataAnnotations.Schema;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Persistence;
using Wfc.Utils;
using Wfc.Utils.Attributes;

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

  #region Nodes
  [NodePath("DeleteButton")]
  private SlotButton _deleteButtonNode = default!;
  [NodePath("ConfirmButton")]
  private SlotButton _confirmButtonNode = default!;
  private Control _spaceNode = default!;
  #endregion Nodes

  public void OnResolved() { }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _spaceNode = new Control();

    GetParent().CallDeferred(Node2D.MethodName.AddChild, _spaceNode);
    _spaceNode.CallDeferred(Node2D.MethodName.SetOwner, GetParent());

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
