using Godot;
using System;

public partial class PowerUpScript : Node2D
{
    [Signal]
    public delegate void is_destroyedEventHandler(PowerUpScript powerUpScript);

    protected BrickBreaker BrickBreakerNode = null;
    public bool IsIncremental = false; // Player can have multiple instances of this power-up

    public void SetBrickBreakerNode(BrickBreaker brickNode)
    {
        BrickBreakerNode = brickNode;
    }

    public void EmitIsDestroyed()
    {
        EmitSignal(nameof(is_destroyedEventHandler), this);
    }

    public virtual bool IsStillRelevant()
    {
        return true;
    }
}
