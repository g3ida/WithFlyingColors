using Godot;
using System;

public class PowerUpScript : Node2D
{
    [Signal]
    public delegate void is_destroyed(PowerUpScript powerUpScript);

    protected BrickBreaker BrickBreakerNode = null;
    public bool IsIncremental = false; // Player can have multiple instances of this power-up

    public void SetBrickBreakerNode(BrickBreaker brickNode)
    {
        BrickBreakerNode = brickNode;
    }

    public void EmitIsDestroyed()
    {
        EmitSignal(nameof(is_destroyed), this);
    }

    public virtual bool IsStillRelevant()
    {
        return true;
    }
}
