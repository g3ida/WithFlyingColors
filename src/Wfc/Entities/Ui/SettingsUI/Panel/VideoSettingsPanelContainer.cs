namespace Wfc.Entities.Ui.SettingsUI.Pamel;

using Godot;
using Wfc.Core.Event;
using Wfc.Core.Settings;
using Wfc.Utils;
using Wfc.Utils.Attributes;

public partial class VideoSettingsPanelContainer : PanelContainer {

  // doesn't work since the node will change in the tree
  [NodePath("MarginContainer/GridContainer/ResolutionAuto")]
  private Control _autoResolution = default!;

  [NodePath("MarginContainer/GridContainer/Resolution")]
  private Control _resolutionSelect = default!;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _hideOrShowAutoResolution();
  }

  private static void _onVsyncCheckboxToggled(bool buttonPressed) {
    GameSettings.Vsync = buttonPressed;
    EventHandler.Instance.EmitVsyncToggled(buttonPressed);
  }

  private void _onFullscreenCheckboxToggled(bool buttonPressed) {
    GameSettings.Fullscreen = buttonPressed;
    EventHandler.Instance.EmitFullscreenToggled(buttonPressed);
    _hideOrShowAutoResolution();
  }

  private void _hideOrShowAutoResolution() {
    if (GameSettings.Fullscreen) {
      _autoResolution.Visible = true;
      _resolutionSelect.Visible = false;
    }
    else {
      _autoResolution.Visible = false;
      _resolutionSelect.Visible = true;
      // fixme: LaunchScheduledRescale(); is this necessary ???
    }
  }


}
