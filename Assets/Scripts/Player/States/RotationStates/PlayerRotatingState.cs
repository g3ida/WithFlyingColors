using Godot;
using System;

public class PlayerRotatingState : PlayerBaseState
{
    private int rotationDirection;
    private PlayerRotationAction playerRotation;

    public PlayerRotatingState() : base() { }

    public void Init(Player player,  int direction)
    {
        playerRotation = player.playerRotationAction;// new PlayerRotationAction();
        playerRotation.Set(player);
        rotationDirection = direction;
        this.baseState = direction == -1 ? PlayerStatesEnum.ROTATING_LEFT : PlayerStatesEnum.ROTATING_RIGHT;
    }

    protected override void _Enter(Player player)
    {
        bool cumulateAngle = player.player_state.baseState != PlayerStatesEnum.SLIPPERING;
        playerRotation.Execute(rotationDirection, Constants.PI2, 0.1f, true, cumulateAngle, true);
        Event.Instance().EmitPlayerRotate(rotationDirection);
    }

    public override BaseState<Player> PhysicsUpdate(Player player, float delta)
    {
        playerRotation.Step(delta);
        if (playerRotation.canRotate)
        {
            return player.states_store.GetState(PlayerStatesEnum.IDLE);
        }
        return HandleRotate(player);
    }

    public BaseState<Player> on_animation_finished(string animName)
    {
        return null;
    }
}
