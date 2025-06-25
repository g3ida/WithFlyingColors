
namespace Wfc.State;

using Godot;

public abstract partial class BaseState<T> : Node {

  public BaseState() {
  }

  public virtual void Init(T o) {

  }

  public virtual void Enter(T o) {
  }

  public virtual void Exit(T o) {
  }

  public virtual void _Input(T o, InputEvent ev) {
  }

  public abstract BaseState<T>? PhysicsUpdate(T o, float delta);
}
