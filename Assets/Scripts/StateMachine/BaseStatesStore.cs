using Godot;
using System;

public abstract partial class BaseStatesStore<T, U> : Node
{
    public abstract BaseState<T> GetState(U state);
}
