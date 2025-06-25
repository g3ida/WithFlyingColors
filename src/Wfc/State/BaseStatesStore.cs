namespace Wfc.State;

using System;
using Godot;

public abstract partial class BaseStatesStore<T1, T2> : Node {
  public abstract BaseState<T1>? GetState(T2 state);
}
