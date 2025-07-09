namespace Wfc.Entities.World.Gems;

using System;
using Godot;
using Wfc.State;

public abstract partial class GemBaseState : BaseState<Gem> {

  public GemBaseState() : base() { }

  public override BaseState<Gem>? PhysicsUpdate(Gem gem, float delta) {
    return null;
  }
}
