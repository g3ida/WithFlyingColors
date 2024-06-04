using Godot;
using System;

public partial class KeyBindingController : Control, IUITab
{
    [Signal]
    public delegate void on_action_bound_signalEventHandler(string action, int key);

    private Button jumpBtn;

    public override void _Ready()
    {
        jumpBtn = GetNode<Button>("GridContainer/JumpBtn");
    }

    private void _on_keyboard_input_action_bound(string action, int key)
    {
        if (key < 0)
        {
            GameSettings.Instance().UnbindActionKey(action);
        }
        else
        {
            GameSettings.Instance().BindActionToKeyboardKey(action, key);
            EmitSignal(nameof(on_action_bound_signalEventHandler), action, key);
            Event.Instance().EmitOnActionBound(action, key);
        }
    }

    public void on_gain_focus()
    {
        jumpBtn.GrabFocus();
    }
}

