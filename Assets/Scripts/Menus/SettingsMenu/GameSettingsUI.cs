using Godot;
using System;

public partial class GameSettingsUI : Control, IUITab {
  private CheckBox vsyncCheckbox;
  private CheckBox fullScreenCheckbox;
  private Label autoResolutionLabel;
  private UISelect resolutionSelect;

  private bool is_ready = false;


  public override void _EnterTree() {
    base._EnterTree();
    // Fixme: make better logic to initialize the resolution select driver. C# migration.
    resolutionSelect = GetNode<UISelect>("GridContainer/ResolutionUISelect");
    resolutionSelect.select_driver = new ResolutionSelectDriver();

  }

  public override void _Ready() {
    vsyncCheckbox = GetNode<CheckBox>("GridContainer/VsyncCheckbox");
    fullScreenCheckbox = GetNode<CheckBox>("GridContainer/FullscreenCheckbox");
    autoResolutionLabel = GetNode<Label>("GridContainer/AutoResolutionLabel");

    vsyncCheckbox.ButtonPressed = GameSettings.Instance().Vsync;
    fullScreenCheckbox.ButtonPressed = GameSettings.Instance().Fullscreen;
    ToggleAutoResolution();
    on_gain_focus();
    is_ready = true;

    // vsyncCheckbox.Connect("toggled", this, nameof(_on_VsyncCheckbox_toggled));
    // fullScreenCheckbox.Connect("toggled", this, nameof(_on_FullscreenCheckbox_toggled));
    // resolutionSelect.Connect("Value_changed", this, nameof(_on_UISelect_Value_changed));
    // resolutionSelect.Connect("selection_changed", this, nameof(_on_ResolutionUISelect_selection_changed));


  }

  private void _on_VsyncCheckbox_toggled(bool buttonPressed) {
    GameSettings.Instance().Vsync = buttonPressed;
    if (is_ready) {
      Event.Instance.EmitVsyncToggled(buttonPressed);
    }
  }

  private void _on_FullscreenCheckbox_toggled(bool buttonPressed) {
    GameSettings.Instance().Fullscreen = buttonPressed;
    if (is_ready) {
      Event.Instance.EmitFullscreenToggled(buttonPressed);
    }
    ToggleAutoResolution();
  }

  private void ToggleAutoResolution() {
    if (GameSettings.Instance().Fullscreen) {
      autoResolutionLabel.Visible = true;
      resolutionSelect.Visible = false;
    }
    else {
      autoResolutionLabel.Visible = false;
      resolutionSelect.Visible = true;
      LaunchScheduledRescale();
    }
  }

  private void _on_UISelect_Value_changed(Vector2I value) {
    //var resolution = (Vector2)GD.Convert(value, Variant.Type.Vector2);
    var resolution = value;
    GameSettings.Instance().WindowSize = resolution;
    if (is_ready) {
      Event.Instance.EmitScreenSizeChanged(resolution);
    }
  }

  private void LaunchScheduledRescale() {
    Timer rescaleTimer = new Timer {
      WaitTime = 0.4f,
      OneShot = true
    };
    rescaleTimer.Connect("timeout", new Callable(this, nameof(on_rescale_timeout)));
    AddChild(rescaleTimer, true);
    rescaleTimer.Start();
  }

  private void on_rescale_timeout() {
    GameSettings.Instance().WindowSize = (Vector2I)resolutionSelect.selected_value;
  }

  public void on_gain_focus() {
    if (resolutionSelect.Visible) {
      resolutionSelect.GrabFocus();
    }
    else {
      fullScreenCheckbox.GrabFocus();
    }
  }

  private void _on_ResolutionUISelect_selection_changed(bool isEdit) {
    if (is_ready) {
      Event.Instance.EmitScreenSizeChanged(GameSettings.Instance().WindowSize);
    }
  }
}
