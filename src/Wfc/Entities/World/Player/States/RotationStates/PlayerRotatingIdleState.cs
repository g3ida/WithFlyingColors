namespace Wfc.Entities.World.Player;

using System;
using Godot;
using Wfc.State;

public partial class PlayerRotatingIdleState : PlayerBaseState {

  public PlayerRotatingIdleState() : base() {
    baseState = PlayerStatesEnum.IDLE;
  }

  protected override void _Enter(Player player) { }

  protected override void _Exit(Player player) { }

  public override BaseState<Player>? PhysicsUpdate(Player player, float delta) {
    player.PlayerRotationAction.Step(delta);
    return HandleRotate(player);
  }
}
