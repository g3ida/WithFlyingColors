namespace Wfc.Entities.Ui.SettingsUI.UISelect;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;

public partial class ResolutionSelectDriver : UISelectDriver {
  private List<Vector2I> _resolutions = new List<Vector2I>();

  public ResolutionSelectDriver() {

    var resolutionNames = new Dictionary<Vector2I, string> {
      [new Vector2I(3840, 2160)] = "4K UHD",
      [new Vector2I(2560, 1440)] = "QHD 2K",
      [new Vector2I(1920, 1080)] = "FHD 1080p",
      [new Vector2I(1280, 720)] = "HD 720p",
      [new Vector2I(1024, 576)] = "SD 576p",
      [new Vector2I(800, 450)] = "SD 450p"
    };

    var screen_size = DisplayServer.ScreenGetSize();
    foreach (var (vec, name) in resolutionNames) {
      if (vec.X <= screen_size.X && vec.Y <= screen_size.Y) {
        Items.Add(name);
        ItemValues.Add(vec);
        _resolutions.Add(vec);
      }
    }
  }

  public override void onItemSelected(Variant? item) {
    // Logic for handling item selection goes here.
  }

  public override int GetDefaultSelectedIndex() {
    var w_size = GameSettings.WindowSize;
    for (int i = 0; i < _resolutions.Count; i++) {
      if (_resolutions[i] == w_size) {
        return i;
      }
    }
    return 0;
  }

  public override void _Ready() {
    base._Ready();
  }
}
