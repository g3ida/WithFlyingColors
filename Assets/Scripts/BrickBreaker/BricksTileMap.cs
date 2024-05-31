using Godot;
using System;
using System.Collections.Generic;

public class BricksTileMap : Node2D
{
    [Signal]
    public delegate void bricks_cleared();

    [Signal]
    public delegate void level_cleared(int level);

    [Export]
    public bool should_instance_bricks { get; set; } = true;

    private Godot.Collections.Array _levels;
    private List<bool> _isLevelCleared;
    private int _leastUnclearedLevel = 0;

    public override void _Ready()
    {
        _levels = GetChildren();
        _InitIsLevelCleared();
    }

    private void _InitIsLevelCleared()
    {
        _isLevelCleared = new List<bool>();
        for (int i = 0; i < _levels.Count; i++)
        {
            _isLevelCleared.Add(!should_instance_bricks);
        }
    }

    private int _GetLeastUnclearedLevel()
    {
        for (int i = 0; i < _isLevelCleared.Count; i++)
        {
            if (!_isLevelCleared[i])
            {
                return i;
            }
        }
        return -1;
    }

    private void _on_level_bricks_cleared(int id)
    {
        _isLevelCleared[id] = true;
        int newUnclearedLevel = _GetLeastUnclearedLevel();
        if (newUnclearedLevel == -1)
        {
            CallDeferred(nameof(_EmitBricksCleared));
        }
        else if (newUnclearedLevel != _leastUnclearedLevel)
        {
            CallDeferred(nameof(_EmitLevelCleared), newUnclearedLevel);
        }
    }

    private void _EmitLevelCleared(int newUnclearedLevel)
    {
        EmitSignal(nameof(level_cleared), newUnclearedLevel);
    }

    private void _EmitBricksCleared()
    {
        EmitSignal(nameof(bricks_cleared));
    }
}
