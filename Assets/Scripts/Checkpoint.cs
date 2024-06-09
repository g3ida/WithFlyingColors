using Godot;
using System;
using System.Collections.Generic;

// deprecated
public partial class Checkpoint : Area2D, IPersistant
{
  [Export]
  public string color_group { get; set; }
  private bool _isChecked = false;
  private Dictionary<string, object> save_data = new Dictionary<string, object>()
    {
        {"is_checked", false}
    };

  public void reset()
  {
    _isChecked = (bool)save_data["is_checked"];
  }

  public Dictionary<string, object> save()
  {
    return save_data;
  }

  public override void _Ready()
  {
    if (color_group == null)
    {
      GD.PushError("ColorGroup cannot be null");
    }
  }

  private void _on_Checkpoint_body_shape_entered(Rid bodyRid, Node body, int bodyShapeIndex, int localShapeIndex)
  {
    if (!_isChecked)
    {
      _isChecked = true;
      save_data["is_checked"] = true;
      GetNode<AnimationPlayer>("CheckHole/CheckDot/AnimationPlayer").Play("Checkpoint");
      Event.Instance.EmitCheckpointReached(this);
    }
  }

  Dictionary<string, object> IPersistant.save()
  {
    return save_data;
  }

  public void load(Dictionary<string, object> save_data)
  {
    this.save_data = save_data;
    reset();
  }
}
