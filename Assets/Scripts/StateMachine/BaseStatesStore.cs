using Godot;
using System;

public abstract class BaseStatesStore : Node
{
    public abstract BaseState GetState(int state);
}
