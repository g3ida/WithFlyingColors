using Godot;
using System;

[Tool]
public class SubMenuItem : Control
{
    [Export] public string text = "";
    [Export] public int id = 0;
    [Export] public Color color;
    [Export] public MenuButtons @event;
    private bool _disabled = false;
    [Export] public bool disabled
    {
        get { return _disabled; }
        set { SetDisabled(value); }
    }

    private Button ButtonNode;

    public override void _Ready()
    {
        ButtonNode = GetNode<Button>("Button");
        ButtonNode.Text = text;
        ButtonNode.Modulate = color;
        FocusMode = FocusModeEnum.None;
        SetDisabled(disabled);
        SetProcess(false);
    }

    // public override void _Process(float delta)
    // {
    //     if (ButtonNode.HasFocus())
    //     {
    //         GrabFocus();
    //     }
    // }

    public void UpdateColors()
    {
        ButtonNode.Modulate = color;
    }

    public void ButtonGrabFocus()
    {
        if (!disabled)
        {
            ButtonNode.GrabFocus();
        }
    }

    // public bool HasFocus()
    // {
    //     return ButtonNode.HasFocus();
    // }

    private void _on_Button_pressed()
    {
        Event.Instance().EmitMenuButtonPressed(@event);
    }

    private void _on_Button_mouse_entered()
    {
        ButtonGrabFocus();
    }

    private void SetDisabled(bool value)
    {
        _disabled = value;
        if (ButtonNode != null)
        {
            ButtonNode.Disabled = value;
            ButtonNode.FocusMode = value ? FocusModeEnum.None : FocusModeEnum.All;
        }
    }

    private bool GetDisabled()
    {
        return _disabled;
    }
}
