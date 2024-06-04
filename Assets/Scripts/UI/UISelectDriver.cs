using Godot;
using System.Collections.Generic;

public partial class UISelectDriver : Node
{
    public List<string> items = new List<string>();
    public List<object> item_values = new List<object>();

    public UISelectDriver()
    {
        // Constructor logic (if any) goes here.
    }

    public virtual void on_item_selected(string item)
    {
        // Logic for handling item selection goes here.
    }

    public virtual int GetDefaultSelectedIndex()
    {
        return 0; // Default index logic can be modified as needed.
    }

    public override void _Ready()
    {
				base._Ready();
    }
}
