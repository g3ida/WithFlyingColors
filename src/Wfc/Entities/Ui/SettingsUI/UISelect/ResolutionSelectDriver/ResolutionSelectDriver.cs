namespace Wfc.Entities.Ui.SettingsUI.UISelect;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Localization;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;

[Meta(typeof(IAutoNode))]
public partial class ResolutionSelectDriver : UISelectDriver {

  // In fullscreen the only item is a translated "Auto", built once, so the list
  // has to be made again for it to follow a language change.
  public override void _Notification(int what) {
    this.Notify(what);
    if (what == NotificationTranslationChanged && IsNodeReady()) {
      _onFullscreenToggled(GameSettings.Fullscreen);
    }
  }

  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();

  private List<Vector2I> _resolutions = new List<Vector2I>();

  public void OnResolved() {
    _onFullscreenToggled(GameSettings.Fullscreen);
  }

  private Dictionary<Vector2I, string> _resolutionNames = new Dictionary<Vector2I, string> {
    [new Vector2I(3840, 2160)] = "4K UHD",
    [new Vector2I(2560, 1440)] = "QHD 2K",
    [new Vector2I(1920, 1080)] = "FHD 1080p",
    [new Vector2I(1280, 720)] = "HD 720p",
    [new Vector2I(1024, 576)] = "SD 576p",
    [new Vector2I(800, 450)] = "SD 450p"
  };

  public ResolutionSelectDriver() { }

  public override void _EnterTree() {
    base._EnterTree();
    // Fixme: need to use dependency injection here
    EventHandler.Instance.Events.FullscreenToggled += _onFullscreenToggled;
  }

  public void _onFullscreenToggled(bool isFullScreen) {
    Items.Clear();
    ItemValues.Clear();
    _resolutions.Clear();
    if (!isFullScreen) {
      var screen_size = DisplayServer.ScreenGetSize();
      foreach (var (vec, name) in _resolutionNames) {
        if (vec.X <= screen_size.X && vec.Y <= screen_size.Y) {
          Items.Add(name);
          ItemValues.Add(vec);
          _resolutions.Add(vec);
        }
      }
    }
    else {
      var autoStr = LocalizationService.GetLocalizedString(TranslationKey.game_settings_display_resolutionAuto);
      Items.Add(autoStr);
      ItemValues.Add(new Vector2I(640, 480));
    }
    EmitSignal(nameof(ItemListChanged));
  }

  public override void onItemSelected(Variant? item) {
    // Logic for handling item selection goes here.
  }

  public override int GetDefaultSelectedIndex() {
    if (!GameSettings.Fullscreen) {
      var w_size = GameSettings.WindowSize;
      for (int i = 0; i < _resolutions.Count; i++) {
        if (_resolutions[i] == w_size) {
          return i;
        }
      }
    }
    return 0;
  }

  public override void _Ready() {
    base._Ready();
  }

  public override void _ExitTree() {
    EventHandler.Instance.Events.FullscreenToggled -= _onFullscreenToggled;
    base._ExitTree();
  }
}
