namespace Wfc.Entities.Ui.SettingsUI.Panel;

using System.Threading.Tasks;
using Godot;
using Wfc.Core.Settings;
using Wfc.Entities.Ui.Dialogs;
using Wfc.Entities.Ui.SettingsUI.UISelect;
using Wfc.Utils;
using Wfc.Utils.Attributes;

public partial class VideoSettingsPanelContainer : PanelContainer {

  // I had to add the "Content" node in the path since the UGridRow adds an extra container
  [NodePath("MarginContainer/UiGridContainer/Resolution/")]
  private Control _resolutionSelectRow = default!;

  [NodePath("MarginContainer/UiGridContainer/Resolution/Content/ResolutionSelectButton")]
  private UIDropdownButton _resolutionSelectButton = default!;

  [NodePath("MarginContainer/UiGridContainer/Fullscreen/Content/FullscreenCheckbox")]
  private CheckBox _fullscreenCheckbox = default!;

  [NodePath("MarginContainer/UiGridContainer/VSync/Content/VSyncCheckbox")]
  private CheckBox _vsyncCheckbox = default!;

  [NodePath("MarginContainer/UiGridContainer/Resizable")]
  private Control _resizableRow = default!;

  [NodePath("MarginContainer/UiGridContainer/Resizable/Content/ResizableCheckbox")]
  private CheckBox _resizableCheckbox = default!;

  [NodePath("MarginContainer/UiGridContainer/PerformanceOverlay/Content/PerformanceOverlayCheckbox")]
  private CheckBox _performanceOverlayCheckbox = default!;

  [NodePath("ResolutionDialogContainer")]
  private DialogContainer _resolutionDialogNode = default!;

  [NodePath("ResolutionDialogContainer/ResolutionDialog")]
  private ConfirmDialog _resolutionConfirmNode = default!;

  // The size the window had before the player picked another one, held for as long
  // as the confirmation is up. A resolution the monitor cannot show leaves them
  // with nothing to read the dialog with, so letting it run out has to put the
  // window back the way an answer of Revert would.
  private Vector2I? _sizeBeforeChange;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _resolutionSelectButton.ValueCommitted += _onResolutionCommitted;
    _resolutionConfirmNode.Confirmed += _onResolutionKept;
    _resolutionDialogNode.Dismissed += _onResolutionReverted;
    _fullscreenCheckbox.Toggled += _onFullscreenCheckboxToggled;
    _vsyncCheckbox.Toggled += _onVsyncCheckboxToggled;
    _resizableCheckbox.Toggled += _onResizableCheckboxToggled;
    _performanceOverlayCheckbox.Toggled += _onPerformanceOverlayCheckboxToggled;
    _fullscreenCheckbox.SetPressed(GameSettings.Fullscreen);
    _vsyncCheckbox.SetPressed(GameSettings.Vsync);
    _resizableCheckbox.SetPressed(GameSettings.Resizable);
    _performanceOverlayCheckbox.SetPressed(GameSettings.PerformanceOverlay);
    _showResizableRow(!GameSettings.Fullscreen);
    // Nothing is applied here on purpose: the window already has the size it was
    // last given, and re-applying it made the window jump back to the middle of
    // the screen every time the player opened the settings.
  }

  public override void _ExitTree() {
    base._ExitTree();
    _resolutionSelectButton.ValueCommitted -= _onResolutionCommitted;
    _resolutionConfirmNode.Confirmed -= _onResolutionKept;
    _resolutionDialogNode.Dismissed -= _onResolutionReverted;
    _fullscreenCheckbox.Toggled -= _onFullscreenCheckboxToggled;
    _vsyncCheckbox.Toggled -= _onVsyncCheckboxToggled;
    _resizableCheckbox.Toggled -= _onResizableCheckboxToggled;
    _performanceOverlayCheckbox.Toggled -= _onPerformanceOverlayCheckboxToggled;
  }

  private static void _onVsyncCheckboxToggled(bool buttonPressed) {
    GameSettings.Vsync = buttonPressed;
    SettingsRepo.Instance.OnVsyncToggled(buttonPressed);
  }

  // A fullscreen window has no edges to take hold of, so the row is taken away
  // there rather than left sitting there doing nothing. The focus manager asks each
  // row whether it is there as it walks, so one that goes mid-panel cannot be landed
  // on and one that comes back is reachable again.
  private void _showResizableRow(bool visible) => _resizableRow.Visible = visible;

  // Dragging an edge is answered by WindowAspectGuard, which keeps whatever the
  // player pulls the window to at the shape the game is drawn in.
  private static void _onResizableCheckboxToggled(bool buttonPressed) =>
    GameSettings.Resizable = buttonPressed;

  // The setting raises the toggle itself, so the overlay follows a settings file that
  // asks for it as well as a player who ticks the box.
  private static void _onPerformanceOverlayCheckboxToggled(bool buttonPressed) =>
    GameSettings.PerformanceOverlay = buttonPressed;

  private async void _onFullscreenCheckboxToggled(bool buttonPressed) {
    GameSettings.Fullscreen = buttonPressed;
    _showResizableRow(!buttonPressed);
    SettingsRepo.Instance.OnFullscreenToggled(buttonPressed);
    _toggleAutoResolution();
    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    // The tree outlives this panel, so unlike an await on one of its own children this one
    // always comes back - on a panel the player may have closed in the meantime, whose
    // GetWindow() is then null.
    if (!IsInsideTree()) {
      return;
    }
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
    // Leaving fullscreen is a change the player made knowingly and can undo by
    // ticking the box again, so the window simply takes the size the row shows.
    _applySelectedResolution();
  }

  // The player picked another resolution out of the open list. It is applied at
  // once - a size cannot be judged from its name - and stands or falls by the
  // confirmation that follows.
  private void _onResolutionCommitted(Variant value) {
    if (GameSettings.Fullscreen || !_isUsableSize(value, out var newSize)) {
      return;
    }
    _sizeBeforeChange = GameSettings.WindowSize;
    _applyWindowSize(newSize);
    _resolutionDialogNode.ShowDialog();
  }

  private void _onResolutionKept() => _sizeBeforeChange = null;

  private void _onResolutionReverted() {
    if (_sizeBeforeChange is not Vector2I previousSize) {
      return;
    }
    _sizeBeforeChange = null;
    _applyWindowSize(previousSize);
    _resolutionSelectButton.SyncSelectionToDefault();
  }

  private void _applySelectedResolution() {
    if (!GameSettings.Fullscreen && _isUsableSize(_resolutionSelectButton.SelectedValue, out var newSize)) {
      _applyWindowSize(newSize);
    }
  }

  private static bool _isUsableSize(Variant? value, out Vector2I size) {
    size = value?.As<Vector2I>() ?? Vector2I.Zero;
    return size.X > 0 && size.Y > 0;
  }

  private async void _applyWindowSize(Vector2I newSize) {
    // Re-centring is part of the size change, GameSettings does it.
    GameSettings.WindowSize = newSize;

    if (this.IsNodeReady()) {
      SettingsRepo.Instance.OnScreenSizeChanged(newSize);
    }
    await _refreshWindow(newSize);
  }

  private async Task _refreshWindow(Vector2I newSize) {
    // Taken once and held, rather than asked for again after each wait: GetTree() answers null
    // on a node that has left the tree, and the waits below are on the tree itself, which
    // always comes back however long the panel lasts.
    var tree = GetTree();
    // hack to force resize immediately, bug happening on Linux https://github.com/godotengine/godot/issues/105597
    await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    tree.Root.Size = newSize;
    tree.Root.ContentScaleFactor = 1.001f;
    await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    tree.Root.ContentScaleFactor = 1.0f;
  }
}
