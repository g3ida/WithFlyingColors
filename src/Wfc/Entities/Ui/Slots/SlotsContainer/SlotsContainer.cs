namespace Wfc.Entities.Ui.Slots;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Persistence;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class SlotsContainer : Control {

  public override void _Notification(int what) => this.Notify(what);
  [Signal]
  public delegate void SlotPressedEventHandler(int id, string action);

  [Export]
  public bool centered_on_screen_v = false;
  [Export]
  public bool centered_on_screen_h = false;

  #region Nodes
  [NodePath("HBoxContainer")]
  private Control _boxContainerNode = default!;
  [NodePath("HBoxContainer/SaveSlot1")]
  private SaveSlotPanel _saveSlot1Node = default!;
  [NodePath("HBoxContainer/SaveSlot2")]
  private SaveSlotPanel _saveSlot2Node = default!;
  [NodePath("HBoxContainer/SaveSlot3")]
  private SaveSlotPanel _saveSlot3Node = default!;
  private SaveSlotPanel[] _saveSlots = default!;
  #endregion Nodes

  public void OnResolved() { }

  [Dependency]
  public ISaveManager SaveManager => this.DependOn<ISaveManager>();

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _saveSlots = new SaveSlotPanel[] { _saveSlot1Node, _saveSlot2Node, _saveSlot3Node };

    SetProcess(false);
    Size = _boxContainerNode.Size;

    for (int i = 0; i < _saveSlots.Length; i++) {
      var texture = SaveManager.GetSlotImage(i);
      if (texture != null) {
        _saveSlots[i].Texture2D = texture;
      }
      _saveSlots[i]._id = i;
      _saveSlots[i].UpdateMetaData();
      _saveSlots[i].SlotIndexLabel = i;
    }

    for (int i = 0; i < _saveSlots.Length; i++) {
      if (!_saveSlots[i].IsDisabled) {
        _saveSlots[i].HasFocus = true;
        break;
      }
    }

    if (centered_on_screen_h) {
      Position = new Vector2((GetViewportRect().Size.X - Size.X) * 0.5f, Position.Y);
    }
    if (centered_on_screen_v) {
      Position = new Vector2(Position.X, (GetViewportRect().Size.Y - Size.Y) * 0.5f);
    }

    _saveSlots[SaveManager.GetSelectedSlotIndex()].SetHasFocus(true);
  }

  private void _onSaveSlot1Pressed(string action) {
    EmitSignal(nameof(SlotPressed), 0, action);
  }

  private void _onSaveSlot2Pressed(string action) {
    EmitSignal(nameof(SlotPressed), 1, action);
  }

  private void _onSaveSlot3Pressed(string action) {
    EmitSignal(nameof(SlotPressed), 2, action);
  }

  public void UpdateSlots() {
    foreach (var slot in _saveSlots) {
      slot.UpdateMetaData();
    }
  }

  public void UpdateSlot(int id, bool setFocus = false) {
    _saveSlots[id].UpdateMetaData();
    if (setFocus) {
      _saveSlots[id].HideActionButtons();
    }
  }

  public void SetGameCurrentSelectedSlot(int index) {
    for (int i = 0; i < _saveSlots.Length; i++) {
      if (index == i) {
        _saveSlots[i].UpdateMetaData();
        _saveSlots[i].SetBorder(true);
        SaveManager.SelectSlot(index);
      }
      else {
        _saveSlots[i].SetBorder(false);
      }
    }
  }
}
