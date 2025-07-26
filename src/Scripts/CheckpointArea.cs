using System.Collections.Generic;
using Godot;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class CheckpointArea : Area2D {
  [Signal]
  public delegate void checkpoint_hitEventHandler();

  [Export]
  public string ColorGroup { get; set; }

  private bool _isChecked = false;
  private Dictionary<string, object> _save_data = new Dictionary<string, object>
    {
        { "is_checked", false }
    };

  public override void _Ready() {
    if (string.IsNullOrEmpty(ColorGroup)) {
      GD.PushError("ColorGroup cannot be null or empty");
    }
  }

  public void Reset() {
    _isChecked = (bool)_save_data["is_checked"];
  }

  public Dictionary<string, object> Save() {
    return _save_data;
  }

  public void _on_CheckpointArea_body_entered(Node2D body) {
    if (body == Global.Instance().Player && !_isChecked) {
      _isChecked = true;
      _save_data["is_checked"] = true;
      EmitSignal(nameof(checkpoint_hit));
      EventHandler.Instance.EmitCheckpointReached(this);
    }
  }

  public Dictionary<string, object> save() {
    return _save_data;
  }

  public void load(Dictionary<string, object> save_data) {
    _save_data = save_data;
    Reset();
  }
}
