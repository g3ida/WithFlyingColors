using Godot;
using System;

public partial class UISliderButton : Button
{
    [Signal]
    public delegate void value_changedEventHandler(float value);

    [Signal]
    public delegate void selection_changedEventHandler(bool isSelected);

    private HSlider SliderNode;
    private AnimationPlayer AnimationPlayerNode;
    private bool isEditing = false;

    public float Value
    {
        get => (float)SliderNode.Value;
        set => SliderNode.Value = value;
    }

    public override void _Ready()
    {
        SliderNode = GetNode<HSlider>("HSlider");
        AnimationPlayerNode = GetNode<AnimationPlayer>("AnimationPlayer");

        SliderNode.FocusMode = FocusModeEnum.None;
        FocusMode = FocusModeEnum.All;

        // SliderNode.Connect("drag_ended", this, nameof(_on_HSlider_drag_ended));
    }

    public override void _Input(InputEvent ev)
    {
        if (HasFocus())
        {
            if (Input.IsActionJustPressed("ui_accept"))
            {
                SetEditing(!isEditing);
                GetViewport().SetInputAsHandled();
            }
            else if (Input.IsActionJustPressed("ui_cancel") && isEditing)
            {
                SetEditing(false);
                GetViewport().SetInputAsHandled();
            }
            else if (isEditing)
            {
                if (Input.IsActionJustPressed("ui_left"))
                {
                    _on_left_pressed();
                    GetViewport().SetInputAsHandled();
                }
                else if (Input.IsActionJustPressed("ui_right"))
                {
                    _on_right_pressed();
                    GetViewport().SetInputAsHandled();
                }
            }
        }
    }

    private void SetEditing(bool value)
    {
        if (!isEditing && value)
        {
            AnimationPlayerNode.Stop();
            AnimationPlayerNode.Play("Blink");
            EmitSelectionChangedSignal();
        }
        else if (isEditing && !value)
        {
            AnimationPlayerNode.Stop();
            AnimationPlayerNode.Play("RESET");
            EmitSelectionChangedSignal();
        }
        isEditing = value;
    }

    private void _on_left_pressed()
    {
        AddValueToSlider(-(float)SliderNode.Step);
    }

    private void _on_right_pressed()
    {
        AddValueToSlider((float)SliderNode.Step);
    }

    private void AddValueToSlider(float value)
    {
        SliderNode.Value = Mathf.Clamp((float)SliderNode.Value + value, (float)SliderNode.MinValue, (float)SliderNode.MaxValue);
        EmitValueChangedSignal();
    }

    private void _on_mouse_entered()
    {
        GrabFocus();
    }

    private void _on_HSlider_drag_ended(double value)
    {
        EmitValueChangedSignal();
    }

    private void EmitValueChangedSignal()
    {
        EmitSignal(nameof(value_changed), SliderNode.Value);
    }

    private void EmitSelectionChangedSignal()
    {
        EmitSignal(nameof(selection_changed), isEditing);
    }
}
