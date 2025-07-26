namespace Wfc.Entities.World.Checkpoints;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class CheckpointArea : Area2D, IPersistent {
  [Signal]
  public delegate void checkpoint_hitEventHandler();

  [Export]
  public string ColorGroup { get; set; } = "blue";

  private bool _isChecked = false;

  private sealed record SaveData(bool isChecked = false);
  private SaveData _saveData = new SaveData();

  public override void _Ready() {
    if (string.IsNullOrEmpty(ColorGroup)) {
      GD.PushError("ColorGroup cannot be null or empty");
    }
  }

  public void Reset() {
    _isChecked = _saveData.isChecked;
  }

  public void _on_CheckpointArea_body_entered(Node2D body) {
    if (body == Global.Instance().Player && !_isChecked) {
      _isChecked = true;
      _saveData = new SaveData(isChecked: true);
      EmitSignal(nameof(checkpoint_hit));
      EventHandler.Instance.EmitCheckpointReached(this);
    }
  }

  public string GetSaveId() => this.GetPath();
  public string Save(ISerializer serializer) => serializer.Serialize(_saveData);
  public void Load(ISerializer serializer, string data) {
    var deserializedData = serializer.Deserialize<SaveData>(data);
    this._saveData = deserializedData ?? new SaveData();
    Reset();
  }
}
