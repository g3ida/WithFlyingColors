using Godot;
using System;

public class SlotActionButtons : HBoxContainer
{
    public enum State { HIDDEN, HIDING, SHOWING, SHOWN }

    [Signal]
    public delegate void select_button_pressed(int slotIndex);
    [Signal]
    public delegate void clear_button_pressed(int slotIndex);

    public int SlotIndex { get; set; } = 0;

    private SlotButton _deleteButtonNode;
    private SlotButton _confirmButtonNode;
    private Control _spaceNode;
    private State _currentState = State.HIDDEN;

    public override void _Ready()
    {
        _deleteButtonNode = GetNode<SlotButton>("DeleteButton");
        _confirmButtonNode = GetNode<SlotButton>("ConfirmButton");
        _spaceNode = new Control();

        GetParent().CallDeferred("add_child", _spaceNode);
        _spaceNode.CallDeferred("set_owner", GetParent());

        UpdateSpaceNode(0);
    }

    public void ShowButton()
    {
        if (ShouldShowDeleteButton())
        {
            _deleteButtonNode.ShowButton();
            _deleteButtonNode.GrabFocus();
        }
        if (ShouldShowSelectButton())
        {
            _confirmButtonNode.ShowButton();
            _confirmButtonNode.GrabFocus();
        }
    }

    public void HideButton()
    {
        _deleteButtonNode.HideButton();
        _confirmButtonNode.HideButton();
    }

    private void UpdateSpaceNode(float buttonsSize)
    {
        float buttonsMaxWidth = SlotButton.BUTTON_MAX_WIDTH * 2;
        _spaceNode.RectSize = new Vector2(buttonsMaxWidth - buttonsSize, _spaceNode.RectSize.y);
        _spaceNode.RectMinSize = new Vector2(buttonsMaxWidth - buttonsSize, _spaceNode.RectMinSize.y);
    }

    public override void _Process(float delta)
    {
        float buttonsSize = _deleteButtonNode.RectSize.x + _confirmButtonNode.RectSize.x;
        UpdateSpaceNode(buttonsSize);
    }

    public bool ButtonsHasFocus()
    {
        return _deleteButtonNode.ButtonHasFocus() || _confirmButtonNode.ButtonHasFocus();
    }

    private void _on_DeleteButton_pressed()
    {
        EmitSignal(nameof(clear_button_pressed), SlotIndex);
    }

    private bool ShouldShowDeleteButton()
    {
        return SaveGame.Instance().IsSlotFilled(SlotIndex);
    }

    private bool ShouldShowSelectButton()
    {
        return SaveGame.Instance().currentSlotIndex != SlotIndex;
    }

    private void _on_ConfirmButton_pressed()
    {
        EmitSignal(nameof(select_button_pressed), SlotIndex);
    }
}
