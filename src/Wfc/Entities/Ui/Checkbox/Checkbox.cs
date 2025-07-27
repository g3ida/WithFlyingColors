namespace Wfc.Entities.Ui;

using System;
using Godot;

public partial class Checkbox : CheckBox {
  public override void _Ready() {
    base._Ready();
  }


  public void _onCheckboxMouseEntered() {
    GrabFocus();
  }
}
