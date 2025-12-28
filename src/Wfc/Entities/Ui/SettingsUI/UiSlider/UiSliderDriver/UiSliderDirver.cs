namespace Wfc.Entities.Ui.SettingsUI.UiSlider;

using System.Collections.Generic;
using Godot;

public partial class UiSliderDirver : Node {

  public virtual void onValueChanged(float value) { }

  public virtual float GetDefaultValue() {
    return 0f;
  }

  public override void _Ready() {
    base._Ready();
  }
}
