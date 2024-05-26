using Godot;

public abstract class BaseState : Node
{
    public BaseState()
    {
    }

    public virtual void Enter()
    {
    }

    public virtual void Exit()
    {
    }

    public override void _Input(InputEvent @event)
    {
    }

    public override void _PhysicsProcess(float delta)
    {
    }
}
