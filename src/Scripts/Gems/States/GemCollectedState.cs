using Godot;
using System;

public partial class GemCollectedState : GemBaseState
{
    public GemCollectedState() : base()
    {
    }

    public override void Enter(Gem gem)
    {
        
        gem.CollisionShapeNode.SetDeferred("disabled", true);
        gem.SetDeferred("visible", false);
    }

    public override void Exit(Gem gem)
    {
        gem.CollisionShapeNode.SetDeferred("disabled", false);
        gem.SetDeferred("visible", true);
    }
}
