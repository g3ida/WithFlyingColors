using Godot;
using System.Collections.Generic;

public class CheckpointArea : Area2D
{
    [Signal]
    public delegate void checkpoint_hit();

    [Export]
    public string ColorGroup { get; set; }

    private bool _isChecked = false;
    private Dictionary<string, object> _saveData = new Dictionary<string, object>
    {
        { "is_checked", false }
    };

    public override void _Ready()
    {
        if (string.IsNullOrEmpty(ColorGroup))
        {
            GD.PushError("ColorGroup cannot be null or empty");
        }
    }

    public void Reset()
    {
        _isChecked = (bool)_saveData["is_checked"];
    }

    public Dictionary<string, object> Save()
    {
        return _saveData;
    }

    public void _OnCheckpointAreaBodyEntered(Node2D body)
    {
        if (body == Global.Instance().Player && !_isChecked)
        {
            _isChecked = true;
            _saveData["is_checked"] = true;
            EmitSignal(nameof(checkpoint_hit));
            Event.Instance().EmitCheckpointReached(this);
        }
    }
}
