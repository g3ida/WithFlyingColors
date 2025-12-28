namespace Wfc.Entities.Ui.SettingsUI.UiSlider;

using System.Collections.Generic;
using Godot;

public partial class MusicSliderDirver : UiSliderDirver {

  public override void onValueChanged(float value) { }

  public override float GetDefaultValue() {
    return 0f;
  }

  public override void _Ready() {
    base._Ready();
  }
}
