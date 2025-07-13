namespace Wfc.State;

using System;
using Godot;

public interface IStatesStore<TStateOwner> {
  TState? GetState<TState>() where TState : class, IState<TStateOwner>;
}
