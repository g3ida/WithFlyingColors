namespace Wfc.Entities.Ui.SettingsUI.Panel;

using System.Drawing;
using System.Threading.Tasks;
using Chickensoft.AutoInject;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Settings;
using Wfc.Entities.Ui.SettingsUI.UISelect;
using Wfc.Utils;
using Wfc.Utils.Attributes;

public partial class VideoSettingsPanelContainer : PanelContainer {

  // I had to add the "Content" node in the path since the UGridRow adds an extra container
  [NodePath("MarginContainer/UiGridContainer/Resolution/")]
  private Control _resolutionSelectRow = default!;

  [NodePath("MarginContainer/UiGridContainer/Resolution/Content/ResolutionSelectButton")]
  private UISelectButton _resolutionSelectButton = default!;

  [NodePath("MarginContainer/UiGridContainer/Fullscreen/Content/FullscreenCheckbox")]
  private CheckBox _fullscreenCheckbox = default!;

  [NodePath("MarginContainer/UiGridContainer/VSync/Content/VSyncCheckbox")]
  private CheckBox _vsyncCheckbox = default!;

  [NodePath("MarginContainer/UiGridContainer/PerformanceOverlay/Content/PerformanceOverlayCheckbox")]
  private CheckBox _performanceOverlayCheckbox = default!;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _resolutionSelectButton.ValueChanged += _onResolutionUISelectValueChanged;
    _fullscreenCheckbox.Toggled += _onFullscreenCheckboxToggled;
    _vsyncCheckbox.Toggled += _onVsyncCheckboxToggled;
    _performanceOverlayCheckbox.Toggled += _onPerformanceOverlayCheckboxToggled;
    _fullscreenCheckbox.SetPressed(GameSettings.Fullscreen);
    _vsyncCheckbox.SetPressed(GameSettings.Vsync);
    _performanceOverlayCheckbox.SetPressed(GameSettings.PerformanceOverlay);
    // Nothing is applied here on purpose: the window already has the size it was
    // last given, and re-applying it made the window jump back to the middle of
    // the screen every time the player opened the settings.
  }

  public override void _ExitTree() {
    base._ExitTree();
    _resolutionSelectButton.ValueChanged -= _onResolutionUISelectValueChanged;
    _fullscreenCheckbox.Toggled -= _onFullscreenCheckboxToggled;
    _vsyncCheckbox.Toggled -= _onVsyncCheckboxToggled;
    _performanceOverlayCheckbox.Toggled -= _onPerformanceOverlayCheckboxToggled;
  }

  private static void _onVsyncCheckboxToggled(bool buttonPressed) {
    GameSettings.Vsync = buttonPressed;
    EventHandler.Instance.EmitVsyncToggled(buttonPressed);
  }

  // The setting raises the toggle itself, so the overlay follows a settings file that
  // asks for it as well as a player who ticks the box.
  private static void _onPerformanceOverlayCheckboxToggled(bool buttonPressed) =>
    GameSettings.PerformanceOverlay = buttonPressed;

  private async void _onFullscreenCheckboxToggled(bool buttonPressed) {
    GameSettings.Fullscreen = buttonPressed;
    EventHandler.Instance.EmitFullscreenToggled(buttonPressed);
    _toggleAutoResolution();
    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    DisplayServer.WindowMoveToForeground();
    GetWindow().MoveToCenter();
    GetWindow().GrabFocus();
  }

  private async void _toggleAutoResolution() {
    if (!GameSettings.Fullscreen) {
      await LaunchScheduledRescale();
    }
  }

  private async Task LaunchScheduledRescale() {
    await ToSignal(GetTree().CreateTimer(0.4f), Timer.SignalName.Timeout);
    // The player can close the settings before the timer runs out.
    if (!IsInsideTree()) {
      return;
    }
    _handleScreenRescale();
  }

  private void _onResolutionUISelectValueChanged(Variant value) {
    _handleScreenRescale();
  }

  private async void _handleScreenRescale() {
    if (!GameSettings.Fullscreen) {
      var sz = _resolutionSelectButton.SelectedValue;
      if (sz?.As<Vector2I>() is Vector2I newSize && newSize.X >= 0 && newSize.Y >= 0) {
        // Re-centring is part of the size change, GameSettings does it.
        GameSettings.WindowSize = newSize;

        if (this.IsNodeReady()) {
          EventHandler.Instance.EmitScreenSizeChanged(newSize);
        }
        await _refreshWindow(newSize);
      }
    }
  }

  private async Task _refreshWindow(Vector2I newSize) {
    // hack to force resize immediately, bug happening on Linux https://github.com/godotengine/godot/issues/105597
    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    GetTree().Root.Size = newSize;
    GetTree().Root.ContentScaleFactor = 1.001f;
    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    GetTree().Root.ContentScaleFactor = 1.0f;
  }
}
