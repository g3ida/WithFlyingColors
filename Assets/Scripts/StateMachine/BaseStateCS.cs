using Godot;

public abstract class BaseStateCS<T> : Node
{
    public BaseStateCS()
    {
    }

    public virtual void Enter(T o)
    {
    }

    public virtual void Exit(T o)
    {
    }

    public virtual void _Input(T o, InputEvent @event)
    {
    }

    public abstract BaseStateCS<T> PhysicsUpdate(T o, float delta);
}
