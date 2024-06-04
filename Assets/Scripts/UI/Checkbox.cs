using Godot;
using System;

public partial class Checkbox : CheckBox
{
    public override void _Ready()
    {
        base._Ready();
    }


    public void _on_Checkbox_mouse_entered()
    {
        GrabFocus();
    }
}
