namespace Wfc.Entities.Ui.Grid;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;

public partial class UIGridContainer : GridContainer {

  public override void _Ready() {
    base._Ready();
    // Make this row stretch across the parent
    SizeFlagsHorizontal = SizeFlags.ExpandFill;
    SizeFlagsVertical = SizeFlags.ShrinkBegin;
  }
}
