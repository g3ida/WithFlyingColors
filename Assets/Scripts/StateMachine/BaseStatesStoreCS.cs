using Godot;
using System;

public abstract class BaseStatesStoreCS<T> : Node
{
    public abstract BaseStateCS<T> GetState(PlayerStatesEnum state);
}
