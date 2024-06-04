using Godot;
using System;

public partial class PlayerRotatingIdleState : PlayerBaseState
{
    private PlayerRotationAction playerRotation;

    public PlayerRotatingIdleState() : base() {
        baseState = PlayerStatesEnum.IDLE;
    }

    protected override void _Enter(Player player)
    {
      playerRotation = player.playerRotationAction;
    }

    protected override void _Exit(Player player)
    {
        // Add any exit logic here
    }

    public override BaseState<Player> PhysicsUpdate(Player player, float delta)
    {
        playerRotation.Step(delta);
        return HandleRotate(player);
    }
}
