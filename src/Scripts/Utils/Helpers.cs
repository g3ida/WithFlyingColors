using System;
using System.Collections.Generic;
using Godot;

public static class Helpers {

  // FIXME please find a better logic for saving data. this is due to inconsistencies
  // for godot and c#
  public static int ParseSaveDataInt(Dictionary<string, object> save_data, string key) {
    return Convert.ToInt32(save_data[key]);
  }

  public static float ParseSaveDataFloat(Dictionary<string, object> save_data, string key) {
    return Convert.ToSingle(save_data[key]);
  }

  public static NodePath ParseSaveDataNodePath(Dictionary<string, object> save_data, string key) {
    return (NodePath)Convert.ToString(save_data[key]);
  }

  public static void TriggerFunctionalCheckpoint() {
    var checkpoint = new CheckpointArea();
    checkpoint.ColorGroup = "blue";
    checkpoint._on_CheckpointArea_body_entered(Global.Instance().Player);
  }
}
