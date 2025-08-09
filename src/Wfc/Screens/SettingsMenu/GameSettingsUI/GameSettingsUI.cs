namespace Wfc.Screens.SettingsMenu;

using System;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;
using Wfc.Entities.Ui.UISelect;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
public partial class GameSettingsUI : Control, IUITab {
  [NodePath("GridContainer/VsyncCheckbox")]
  private CheckBox _vsyncCheckbox = default!;
  [NodePath("GridContainer/FullscreenCheckbox")]
  private CheckBox _fullScreenCheckbox = default!;
  [NodePath("GridContainer/AutoResolutionLabel")]
  private Label _autoResolutionLabel = default!;
  [NodePath("GridContainer/ResolutionUISelect")]
  private UISelectButton _resolutionSelect = default!;

  private bool _isReady = false;


  public override void _EnterTree() {
    base._EnterTree();
    this.WireNodes();
    // FIXME: make better logic to initialize the resolution select driver.
    _resolutionSelect.SelectDriver = new ResolutionSelectDriver();
  }

  public override void _Ready() {
    base._Ready();
    // Fixme: make better logic to initialize the resolution select driver. C# migration.
    _vsyncCheckbox.ButtonPressed = GameSettings.Vsync;
    _fullScreenCheckbox.ButtonPressed = GameSettings.Fullscreen;
    ToggleAutoResolution();
    OnGainFocus();
    _isReady = true;
  }

  private void _onVsyncCheckboxToggled(bool buttonPressed) {
    GameSettings.Vsync = buttonPressed;
    if (_isReady) {
      EventHandler.Instance.EmitVsyncToggled(buttonPressed);
    }
  }

  private void _onFullscreenCheckboxToggled(bool buttonPressed) {
    GameSettings.Fullscreen = buttonPressed;
    if (_isReady) {
      EventHandler.Instance.EmitFullscreenToggled(buttonPressed);
    }
    ToggleAutoResolution();
  }

  private void ToggleAutoResolution() {
    if (GameSettings.Fullscreen) {
      _autoResolutionLabel.Visible = true;
      _resolutionSelect.Visible = false;
    }
    else {
      _autoResolutionLabel.Visible = false;
      _resolutionSelect.Visible = true;
      LaunchScheduledRescale();
    }
  }

  private void _onUISelectValueChanged(Vector2I value) {
    //var resolution = (Vector2)GD.Convert(value, Variant.Type.Vector2);
    var resolution = value;
    GameSettings.WindowSize = resolution;
    if (_isReady) {
      EventHandler.Instance.EmitScreenSizeChanged(resolution);
    }
  }

  private void LaunchScheduledRescale() {
    Timer rescaleTimer = new Timer {
      WaitTime = 0.4f,
      OneShot = true
    };
    rescaleTimer.Connect(
      Timer.SignalName.Timeout,
      new Callable(this, nameof(onRescaleTimeout))
    );
    AddChild(rescaleTimer, true);
    rescaleTimer.Start();
  }

  private void onRescaleTimeout() {
    if (_resolutionSelect.SelectedValue != null) {
      GameSettings.WindowSize = (Vector2I)(_resolutionSelect.SelectedValue);
    }
  }

  public void OnGainFocus() {
    if (_resolutionSelect.Visible) {
      _resolutionSelect.GrabFocus();
    }
    else {
      _fullScreenCheckbox.GrabFocus();
    }
  }

  private void _onResolutionUISelectSelectionChanged(bool isEdit) {
    if (_isReady) {
      EventHandler.Instance.EmitScreenSizeChanged(GameSettings.WindowSize);
    }
  }
}
