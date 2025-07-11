namespace Wfc.State;

using System;
using Godot;

public interface IStatesStore<T1, T2> {
  IState<T1>? GetState(T2 state);
}
