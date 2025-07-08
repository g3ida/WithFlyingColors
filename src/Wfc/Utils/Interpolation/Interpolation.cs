namespace Wfc.Utils.Interpolation;

using System;
using Godot;

public abstract class Interpolation {

  public float Apply(float start, float end, float a) {
    return start + (end - start) * ApplyInternal(a);
  }

  protected virtual float ApplyInternal(float a) {
    return 0.0f;
  }
}
