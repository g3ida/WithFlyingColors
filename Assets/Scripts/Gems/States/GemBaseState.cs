using Godot;
using System;

public abstract partial class GemBaseState : BaseState<Gem>
{

    public GemBaseState() : base() { }

    public override BaseState<Gem> PhysicsUpdate(Gem gem, float delta)
    {
        return null;
    }

    public virtual BaseState<Gem> OnCollisionWithBody(Gem gem, Area2D area)
    {
        return null;
    }

    public virtual BaseState<Gem> OnAnimationFinished(Gem gem, string animName)
    {
        return null;
    }
}
