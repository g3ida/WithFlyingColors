namespace Wfc.Entities.Ui;

using System;
using Godot;

public partial class Checkbox : CheckBox {
  // Checkbox doesn't need edit mode - toggle is immediate on confirm
  // SelectionChanged is not needed for checkbox since it toggles immediately

  public override void _Ready() {
    base._Ready();
    FocusMode = FocusModeEnum.All;
  }

  public void _onCheckboxMouseEntered() {
    GrabFocus();
  }
}
