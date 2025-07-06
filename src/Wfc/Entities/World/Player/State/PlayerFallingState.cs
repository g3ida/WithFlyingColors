namespace Wfc.Entities.World.Player;

using System;
using Godot;
using Wfc.State;

public partial class PlayerFallingState : PlayerBaseState {
  private CountdownTimer permissivenessTimer = new CountdownTimer();
  public bool wasOnFloor = false;
  private const float PERMISSIVENESS = 0.04f;

  public PlayerFallingState() : base() {
    permissivenessTimer.Set(PERMISSIVENESS, false);
    this.baseState = PlayerStatesEnum.FALLING;
  }

  protected override void _Enter(Player player) {
    player.AnimatedSpriteNode.Play("idle");
    player.AnimatedSpriteNode.Stop();
    if (wasOnFloor) {
      permissivenessTimer.Reset();
    }
    player.JumpParticlesNode.Emitting = true;
  }

  protected override void _Exit(Player player) {
    permissivenessTimer.Stop();
    player.JumpParticlesNode.Emitting = false;
  }

  protected override BaseState<Player>? _PhysicsUpdate(Player player, float delta) {
    if (player.IsOnFloor()) {
      return player.StatesStore.GetState(PlayerStatesEnum.STANDING);
    }
    if (JumpPressed(player) && permissivenessTimer.IsRunning()) {
      permissivenessTimer.Stop();
      return OnJump(player);
    }
    permissivenessTimer.Step(delta);
    return null;
  }

  public BaseState<Player>? OnAnimationFinished(string animName) {
    return null;
  }
}
