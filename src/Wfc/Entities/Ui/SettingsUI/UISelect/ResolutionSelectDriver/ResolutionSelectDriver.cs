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
      var screenSize = DisplayServer.ScreenGetSize();
      foreach (var (vec, name) in _resolutionNames) {
        if (vec.X <= screenSize.X && vec.Y <= screenSize.Y) {
          Items.Add(name);
          ItemValues.Add(vec);
          _resolutions.Add(vec);
        }
      }
      // On a screen none of the named sizes can cover — a compositor that reports
      // the desktop scaled up, a monitor bigger than 4K — windowed mode is left
      // with nothing that fills it. The screen gets its own entry at the head of
      // the list in that case, and only then: on any ordinary display the named
      // sizes already reach as far, and a second way to say the same thing is
      // just one more item to scroll past.
      var fitToScreen = _getLargestWindowSizeThatFits(screenSize);
      if (_resolutions.Count == 0 || _isLargerThan(fitToScreen, _resolutions[0])) {
        Items.Insert(0, $"{fitToScreen.X}x{fitToScreen.Y}");
        ItemValues.Insert(0, fitToScreen);
        _resolutions.Insert(0, fitToScreen);
      }
    }
    else {
      var autoStr = LocalizationService.GetLocalizedString(TranslationKey.game_settings_display_resolutionAuto);
      Items.Add(autoStr);
      ItemValues.Add(new Vector2I(640, 480));
    }
    EmitSignal(nameof(ItemListChanged));
  }

  private static bool _isLargerThan(Vector2I size, Vector2I other) => size.X * size.Y > other.X * other.Y;

  // The biggest window this screen can hold, kept at the ratio the game is drawn
  // at. The room to work with is the screen less what the desktop keeps for
  // itself (panels, docks) and less the frame the window manager draws, or the
  // title bar's worth of window would hang off the bottom edge. Growing the base
  // viewport into it rather than taking the room as it comes matters on a screen
  // that is not 16:9: the stretch mode is keep_height, so a wider window widens
  // the view, and the menus are laid out for 1920 wide with only so much bleed
  // to give.
  private static Vector2I _getLargestWindowSizeThatFits(Vector2I screenSize) {
    var usable = DisplayServer.ScreenGetUsableRect(DisplayServer.WindowGetCurrentScreen());
    var room = usable.Size == Vector2I.Zero ? screenSize : usable.Size;
    room -= DisplayServer.WindowGetSizeWithDecorations() - DisplayServer.WindowGetSize();

    var baseSize = new Vector2I(
      (int)ProjectSettings.GetSetting("display/window/size/viewport_width"),
      (int)ProjectSettings.GetSetting("display/window/size/viewport_height")
    );
    var scale = Mathf.Min((float)room.X / baseSize.X, (float)room.Y / baseSize.Y);
    var fitted = new Vector2I(Mathf.FloorToInt(baseSize.X * scale), Mathf.FloorToInt(baseSize.Y * scale));
    return fitted.Clamp(Vector2I.One, screenSize);
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
