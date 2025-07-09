namespace Wfc.Entities.World.Gems;

using System;
using Godot;

public partial class GemCollectedState : GemBaseState {
  public GemCollectedState() : base() {
  }

  public override void Enter(Gem gem) {
    gem.CollisionShapeNode.SetDeferred(CollisionPolygon2D.PropertyName.Disabled, true);
    gem.SetDeferred(Area2D.PropertyName.Visible, false);
  }

  public override void Exit(Gem gem) {
    gem.CollisionShapeNode.SetDeferred(CollisionPolygon2D.PropertyName.Disabled, false);
    gem.SetDeferred(Area2D.PropertyName.Visible, true);
  }
}
