using Godot;
using System.Collections.Generic;

public class ResolutionSelectDriver : UISelectDriver
{
    private List<Vector2> resolutions = new List<Vector2>();

    public ResolutionSelectDriver()
    {
        var vals = new List<Vector2>
        {
            new Vector2(1920, 1080),
            new Vector2(1280, 720),
            new Vector2(1024, 576),
            new Vector2(800, 450)
        };

        var screen_size = OS.GetScreenSize();
        foreach (var el in vals)
        {
            if (el.x <= screen_size.x && el.y <= screen_size.y)
            {
                items.Add($"{el.x}x{el.y}");
                item_values.Add(el);
                resolutions.Add(el);
            }
        }
    }

    public override void on_item_selected(string item)
    {
        // Logic for handling item selection goes here.
    }

    public override int GetDefaultSelectedIndex()
    {
        var w_size = GameSettings.Instance().WindowSize;
        for (int i = 0; i < resolutions.Count; i++)
        {
            if (resolutions[i] == w_size)
            {
                return i;
            }
        }
        return 0;
    }

    public override void _Ready()
    {
        base._Ready();
    }
}
