using Godot;

public abstract class BaseState<T> : Node
{
    public BaseState()
    {
    }

    public virtual void Init(T o) {
        
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

    public abstract BaseState<T> PhysicsUpdate(T o, float delta);
}
