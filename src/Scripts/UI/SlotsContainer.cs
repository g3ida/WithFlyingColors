using Godot;
using System;

public partial class SlotsContainer : Control {
    [Signal]
    public delegate void slot_pressedEventHandler(int id, string action);

    [Export]
    public bool centered_on_screen_v = false;
    [Export]
    public bool centered_on_screen_h = false;

    private Control _boxContainerNode;
    private SaveSlot _saveSlot1Node;
    private SaveSlot _saveSlot2Node;
    private SaveSlot _saveSlot3Node;
    private SaveSlot[] _saveSlots;

    public override void _Ready() {
        _boxContainerNode = GetNode<Control>("HBoxContainer");
        _saveSlot1Node = GetNode<SaveSlot>("HBoxContainer/SaveSlot1");
        _saveSlot2Node = GetNode<SaveSlot>("HBoxContainer/SaveSlot2");
        _saveSlot3Node = GetNode<SaveSlot>("HBoxContainer/SaveSlot3");
        _saveSlots = new SaveSlot[] { _saveSlot1Node, _saveSlot2Node, _saveSlot3Node };

        SetProcess(false);
        Size = _boxContainerNode.Size;

        for (int i = 0; i < _saveSlots.Length; i++) {
            _saveSlots[i].Texture2D = SaveGame.Instance().LoadSlotImage(i);
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

        _saveSlots[SaveGame.Instance().CurrentSlotIndex].SetHasFocus(true);

        // _saveSlot1Node.Connect("pressed", this, nameof(OnSaveSlot1Pressed));
        // _saveSlot2Node.Connect("pressed", this, nameof(OnSaveSlot2Pressed));
        // _saveSlot3Node.Connect("pressed", this, nameof(OnSaveSlot3Pressed));
    }

    private void _on_SaveSlot1_pressed(string action) {
        EmitSignal(nameof(slot_pressed), 0, action);
    }

    private void _on_SaveSlot2_pressed(string action) {
        EmitSignal(nameof(slot_pressed), 1, action);
    }

    private void _on_SaveSlot3_pressed(string action) {
        EmitSignal(nameof(slot_pressed), 2, action);
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
                SaveGame.Instance().UpdateSlotLoadDate(index);
            }
            else {
                _saveSlots[i].SetBorder(false);
            }
        }
    }
}
