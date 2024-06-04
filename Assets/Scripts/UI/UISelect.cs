using Godot;
using System;

public partial class UISelect : Button
{
    public UISelectDriver select_driver;

    private HBoxContainer ChildContainerNode;
    private Button LeftArrowNode;
    private Button RightArrowNode;
    private Label LabelNode;
    private AnimationPlayer AnimationPlayerNode;

    private int index;
    public object selected_value;
    private bool is_ready = false;
    private bool is_in_edit_mode = false;

    [Signal]
    public delegate void Value_changedEventHandler(Vector2 value); //FIXME: must work with any type. c# migration.

    [Signal]
    public delegate void selection_changedEventHandler(bool is_edit);

    public override void _Ready()
    {
        ChildContainerNode = GetNode<HBoxContainer>("HBoxContainer");
        LeftArrowNode = GetNode<Button>("HBoxContainer/Left");
        RightArrowNode = GetNode<Button>("HBoxContainer/Right");
        LabelNode = GetNode<Label>("HBoxContainer/Label");
        AnimationPlayerNode = GetNode<AnimationPlayer>("AnimationPlayer");

        index = select_driver.GetDefaultSelectedIndex();
        UpdateSelectedItem();
        UpdateRectSize();
        SetProcess(false);
        is_ready = true;
    }

    public override void _Input(InputEvent ev)
    {
        if (HasFocus())
        {
            if (is_in_edit_mode)
            {
                if (Input.IsActionJustPressed("ui_left"))
                {
                    _on_Left_pressed();
                    GetViewport().SetInputAsHandled();
                }
                else if (Input.IsActionJustPressed("ui_right"))
                {
                    _on_Right_pressed();
                    GetViewport().SetInputAsHandled();
                }
            }

            if (Input.IsActionJustPressed("ui_accept"))
            {
                SetEditMode(!is_in_edit_mode);
                GetViewport().SetInputAsHandled();
            }
            else if (Input.IsActionJustPressed("ui_cancel") && is_in_edit_mode)
            {
                SetEditMode(false);
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void SetEditMode(bool value)
    {
        if (is_in_edit_mode && !value)
        {
            AnimationPlayerNode.Stop();
            AnimationPlayerNode.Play("RESET");
            EmitSelectionChangedSignal();
        }
        if (!is_in_edit_mode && value)
        {
            AnimationPlayerNode.Stop();
            AnimationPlayerNode.Play("Blink");
            EmitSelectionChangedSignal();
        }
        is_in_edit_mode = value;
    }

    private void _on_Left_pressed()
    {
        GrabFocus();
        index = (index + 1) % (int)select_driver.items.Count;
        UpdateSelectedItem();
        EmitSignal(nameof(Value_changed), (Vector2)select_driver.item_values[index]);
    }

    private void _on_Right_pressed()
    {
        GrabFocus();
        index = (index - 1 + select_driver.items.Count) % select_driver.items.Count;
        UpdateSelectedItem();
        EmitSignal(nameof(Value_changed), (Vector2)select_driver.item_values[index]);
    }

    private void UpdateSelectedItem()
    {
        LabelNode.Text = select_driver.items[index];
        select_driver.on_item_selected(LabelNode.Text);
        selected_value = select_driver.item_values[index];
        UpdateRectSize();
    }

    private void UpdateRectSize()
    {
        SetDeferred("rect_min_size", ChildContainerNode.Size);
        SetDeferred("rect_size", ChildContainerNode.Size);
    }

    private void EmitSelectionChangedSignal()
    {
        EmitSignal(nameof(selection_changed), is_in_edit_mode);
    }

    private void _on_Button_mouse_entered()
    {
        GrabFocus();
    }

    private void _on_Label_resized()
    {
        if (is_ready)
        {
            UpdateRectSize();
        }
    }
}
