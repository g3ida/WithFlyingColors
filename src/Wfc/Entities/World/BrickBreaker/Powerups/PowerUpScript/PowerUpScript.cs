namespace Wfc.Entities.World.BrickBreaker.Powerups;

using System;
using Godot;

public partial class PowerUpScript : Node2D {
  [Signal]
  public delegate void IsDestroyedEventHandler(PowerUpScript powerUpScript);

  protected BrickBreaker? BrickBreakerNode = null;
  public bool IsIncremental = false; // Player can have multiple instances of this power-up

  public void SetBrickBreakerNode(BrickBreaker brickNode) {
    BrickBreakerNode = brickNode;
  }

  public void EmitIsDestroyed() {
    EmitSignal(PowerUpScript.SignalName.IsDestroyed, this);
  }

  public virtual bool IsStillRelevant() {
    return true;
  }
}
