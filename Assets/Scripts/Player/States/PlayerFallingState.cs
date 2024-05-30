using Godot;
using System;

public class PlayerFallingState : PlayerBaseStateCS
{
    private CountdownTimer permissivenessTimer = new CountdownTimer();
    public bool wasOnFloor = false;
    private const float PERMISSIVENESS = 0.04f;

    public PlayerFallingState() : base() {
        permissivenessTimer.Set(PERMISSIVENESS, false);
        this.baseState = PlayerStatesEnum.FALLING;
    }

    protected override void _Enter(Player player)
    {
        player.animatedSpriteNode.Play("idle");
        player.animatedSpriteNode.Playing = false;
        if (wasOnFloor)
        {
            permissivenessTimer.Reset();
        }
        player.jumpParticlesNode.Emitting = true;
    }

    protected override void _Exit(Player player)
    {
        permissivenessTimer.Stop();
        player.jumpParticlesNode.Emitting = false;
    }

    protected override BaseState<Player> _PhysicsUpdate(Player player, float delta)
    {
        if (player.IsOnFloor())
        {
            return player.states_store.GetState(PlayerStatesEnum.STANDING);
        }
        if (JumpPressed(player) && permissivenessTimer.IsRunning())
        {
            permissivenessTimer.Stop();
            return OnJump(player);
        }
        permissivenessTimer.Step(delta);
        return null;
    }

    public BaseState<Player> OnAnimationFinished(string animName)
    {
        return null;
    }
}
