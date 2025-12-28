namespace Wfc.Entities.Ui.SettingsUI.Pamel;

using Godot;
using Wfc.Core.Event;
using Wfc.Core.Settings;
using Wfc.Utils;
using Wfc.Utils.Attributes;

public partial class AudioSettingsPanelContainer : PanelContainer {


  public override void _Ready() {
    base._Ready();
    this.WireNodes();
  }

  private static void _onSfxSliderValueChanged(float value) {
    GameSettings.SfxVolume = value;
  }

  private static void _onMusicSliderValueChanged(float value) {
    GameSettings.MusicVolume = value;
  }

}
