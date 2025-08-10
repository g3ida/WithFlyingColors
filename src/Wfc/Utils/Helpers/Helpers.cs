namespace Wfc.Utils;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Entities.World.Checkpoints;
using Wfc.Utils.Colors;

public static class Helpers {

  public static void TriggerFunctionalCheckpoint() {
    var checkpoint = new CheckpointArea();
    checkpoint.ColorGroup = ColorUtils.BLUE;
    checkpoint._on_CheckpointArea_body_entered(Global.Instance().Player);
  }
}
