using Godot;
using System;

public abstract partial class Interpolation : Node // node inheritance is useless
{
    public Interpolation() {
        // Constructor logic if any
    }

    public float Apply(float start, float end, float a) {
        return start + (end - start) * ApplyInternal(a);
    }

    protected virtual float ApplyInternal(float a) {
        return 0.0f;
    }
}