namespace Wfc.Entities.World.Checkpoints;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using EventHandler = Wfc.Core.Event.EventHandler;

// deprecated
public partial class Checkpoint : Area2D, IPersistent {
  [Export]
  public string ColorGroup { get; set; } = "blue";
  private bool _isChecked = false;

  private sealed record SaveData(bool isChecked = false);
  private SaveData _saveData = new SaveData();
  public void Reset() {
    _isChecked = _saveData.isChecked;
  }

  public override void _Ready() {
    if (ColorGroup == null) {
      GD.PushError("ColorGroup cannot be null");
    }
  }

  private void _onCheckpointBodyShapeEntered(Rid bodyRid, Node body, int bodyShapeIndex, int localShapeIndex) {
    if (!_isChecked) {
      _isChecked = true;
      _saveData = new SaveData(isChecked: true);
      GetNode<AnimationPlayer>("CheckHole/CheckDot/AnimationPlayer").Play("Checkpoint");
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
