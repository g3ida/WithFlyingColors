namespace Wfc.Entities.Ui;

using Godot;
using Wfc.Utils;

public partial class Checkbox : CheckBox {
  public override void _Ready() {
    base._Ready();
    FocusMode = FocusModeEnum.All;
    this.GrabFocusOnHover();
  }
}
