namespace Wfc.Entities.Ui.SettingsUI.UISelect;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Chickensoft.Sync.Primitives;
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

  private AutoChannel.Binding? _fullscreenBinding;

  public void OnResolved() {
    _onFullscreenToggled(GameSettings.Fullscreen);
  }

  // Largest first, and 4K is as large as the game is offered at. A size worked out
  // from the screen used to be offered above these, but what the desktop reports is
  // not reliably the number of pixels the player has - a scaled desktop gives a size
  // no monitor is - and an option nobody can name is worse than a ceiling.
  private static readonly (Vector2I Size, string Name)[] RESOLUTIONS = [
    (new Vector2I(3840, 2160), "4K UHD"),
    (new Vector2I(2560, 1440), "QHD 2K"),
    (new Vector2I(1920, 1080), "FHD 1080p"),
    (new Vector2I(1280, 720), "HD 720p"),
    (new Vector2I(1024, 576), "SD 576p"),
    (new Vector2I(800, 450), "SD 450p"),
  ];

  public ResolutionSelectDriver() { }

  public override void _EnterTree() {
    base._EnterTree();
    // The shared instance rather than a dependency: this runs before AutoInject has resolved
    // anything, which is the whole reason the subscription lives here.
    _fullscreenBinding ??= SettingsRepo.Instance.Channel.Bind()
      .On((in ISettingsRepo.FullscreenToggled message) => _onFullscreenToggled(message.IsFullscreen));
  }

  public void _onFullscreenToggled(bool isFullScreen) {
    Items.Clear();
    ItemValues.Clear();
    _resolutions.Clear();
    if (!isFullScreen) {
      var screenSize = DisplayServer.ScreenGetSize();
      foreach (var (size, name) in RESOLUTIONS) {
        if (size.X <= screenSize.X && size.Y <= screenSize.Y) {
          Items.Add(name);
          ItemValues.Add(size);
          _resolutions.Add(size);
        }
      }
      // A screen that reports smaller than every one of them - a desktop the
      // compositor scales, a run with no screen at all - would leave the row with
      // nothing to show. The smallest is offered regardless; applying it clamps to
      // whatever room there turns out to be.
      if (_resolutions.Count == 0) {
        var (size, name) = RESOLUTIONS[^1];
        Items.Add(name);
        ItemValues.Add(size);
        _resolutions.Add(size);
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

  // The nearest of the offered sizes rather than only an exact one: a window the
  // player has dragged the edge of is whatever shape they let go of it at, and the
  // row still has to name something close to what they are looking at.
  public override int GetDefaultSelectedIndex() {
    if (GameSettings.Fullscreen || _resolutions.Count == 0) {
      return 0;
    }
    var windowSize = GameSettings.WindowSize;
    var nearest = 0;
    for (var i = 1; i < _resolutions.Count; i++) {
      if (_areaGap(_resolutions[i], windowSize) < _areaGap(_resolutions[nearest], windowSize)) {
        nearest = i;
      }
    }
    return nearest;
  }

  private static long _areaGap(Vector2I size, Vector2I other) =>
      System.Math.Abs(((long)size.X * size.Y) - ((long)other.X * other.Y));

  public override void _Ready() {
    base._Ready();
  }

  public override void _ExitTree() {
    _fullscreenBinding?.Dispose();
    _fullscreenBinding = null;
    base._ExitTree();
  }
}
