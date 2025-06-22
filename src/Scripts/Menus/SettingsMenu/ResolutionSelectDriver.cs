using System.Collections.Generic;
using Godot;
using Wfc.Core.Settings;

public partial class ResolutionSelectDriver : UISelectDriver {
  private List<Vector2I> resolutions = new List<Vector2I>();

  public ResolutionSelectDriver() {
    var values = new List<Vector2I>
    {
            new Vector2I(1920, 1080),
            new Vector2I(1280, 720),
            new Vector2I(1024, 576),
            new Vector2I(800, 450)
        };

    var screen_size = DisplayServer.ScreenGetSize();
    foreach (var el in values) {
      if (el.X <= screen_size.X && el.Y <= screen_size.Y) {
        items.Add($"{el.X}x{el.Y}");
        item_values.Add(el);
        resolutions.Add(el);
      }
    }
  }

  public override void on_item_selected(string item) {
    // Logic for handling item selection goes here.
  }

  public override int GetDefaultSelectedIndex() {
    var w_size = GameSettings.WindowSize;
    for (int i = 0; i < resolutions.Count; i++) {
      if (resolutions[i] == w_size) {
        return i;
      }
    }
    return 0;
  }

  public override void _Ready() {
    base._Ready();
  }
}
