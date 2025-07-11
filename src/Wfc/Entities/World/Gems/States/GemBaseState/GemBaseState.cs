namespace Wfc.Entities.World.Gems;

using System;
using Godot;
using Wfc.State;

public abstract class GemBaseState : IState<Gem> {
  public abstract void Init(Gem o);
  public abstract void Enter(Gem o);
  public abstract void Exit(Gem o);
  public abstract IState<Gem>? PhysicsUpdate(Gem gem, float delta);
}
