
namespace Wfc.State;

using Godot;

public interface IState<T> {
  void Init(T o);
  void Enter(T o);
  void Exit(T o);
  IState<T>? PhysicsUpdate(T o, float delta);
}
