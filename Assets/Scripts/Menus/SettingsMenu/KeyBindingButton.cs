using Godot;
using System;
using System.Linq;

public class KeyBindingButton : Button
{
    private const string DefaultText = "(EMPTY)";
    [Export] public string key { get; set; }
    private int? value;

    private bool isListening = false;

    [Signal]
    public delegate void keyboard_action_bound(string action, int key);

    private AnimationPlayer AnimationPlayer;

    public override void _Ready()
    {
        AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        var actionList = InputMap.GetActionList(key).Cast<InputEvent>();
        var inputEvent = InputUtils.GetFirstKeyKeyboardEventFromActionList(actionList);
        if (inputEvent != null)
        {
            if (inputEvent is InputEventKey inputKeyEvent)
            {
                value = (int)inputKeyEvent.Scancode;
                Text = OS.GetScancodeString((uint)value.Value);
            }
        }
        AnimationPlayer.Play("RESET");
    }

    private void Undo()
    {
        if (value != null)
        {
            Text = OS.GetScancodeString((uint)value.Value);
        }
        else
        {
            Text = DefaultText;
        }
    }

    public override void _Input(InputEvent @event)
    {
        bool handled = false;
        if (!isListening)
        {
            return;
        }
        if (@event is InputEventKey eventKey)
        {
          value = (int)eventKey.Scancode;
          Text = OS.GetScancodeString((uint)value.Value);
          int valueToSend = value ?? -1;
          EmitSignal(nameof(keyboard_action_bound), key, valueToSend);
          handled = true;
        }
        else if (@event is InputEventMouse eventMouse)
        {
          if ((eventMouse.ButtonMask & (int)ButtonList.Left) == (int)ButtonList.Left)
          {
            Undo();
            handled = true;
          }
        }
        if (handled)
        {
            Pressed = false;
            isListening = false;
            GetTree().SetInputAsHandled();
            GetTree().Paused = false;
            AnimationPlayer.Play("RESET");
        }
    }

    public bool IsValid()
    {
        return Text == DefaultText;
    }

    private void _on_Control_on_action_bound_signal(string action, int key)
    {
        if (action == this.key || key != value)
        {
            return;
        }
        value = null;
        Text = DefaultText;
        EmitSignal(nameof(keyboard_action_bound), action, -1);
    }

    private void _on_KeyBindingButton_pressed()
    {
        if (Pressed)
        {
            Pressed = true;
            isListening = true;
            Event.Instance().EmitKeyboardActionBiding();
            AnimationPlayer.Play("Blink");
            GetTree().Paused = true;
        }
    }

    private void _on_KeyBindingButton_mouse_entered()
    {
        if (!GetTree().Paused)
        {
            GrabFocus();
        }
    }
}
