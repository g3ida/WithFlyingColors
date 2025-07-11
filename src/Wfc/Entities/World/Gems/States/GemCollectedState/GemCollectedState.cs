namespace Wfc.Entities.World.Gems;

using System;
using Godot;
using Wfc.State;

public partial class GemCollectedState : GemBaseState {
  public GemCollectedState() : base() {
  }

  public override void Enter(Gem o) {
    o.CollisionShapeNode.SetDeferred(CollisionPolygon2D.PropertyName.Disabled, true);
    o.SetDeferred(Area2D.PropertyName.Visible, false);
  }

  public override void Exit(Gem o) {
    o.CollisionShapeNode.SetDeferred(CollisionPolygon2D.PropertyName.Disabled, false);
    o.SetDeferred(Area2D.PropertyName.Visible, true);
  }

  public override void Init(Gem o) { }
  public override IState<Gem>? PhysicsUpdate(Gem gem, float delta) { return null; }
}
