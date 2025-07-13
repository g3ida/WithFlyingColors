namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.State;

public partial class PlayerFallingState : PlayerBaseState {
  private CountdownTimer permissivenessTimer = new CountdownTimer();
  public bool wasOnFloor = false;
  private const float PERMISSIVENESS = 0.04f;

  public PlayerFallingState(IPlayerStatesStore statesStore, IInputManager inputManager)
    : base(statesStore, inputManager) {
    permissivenessTimer.Set(PERMISSIVENESS, false);
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

  protected override IState<Player>? _PhysicsUpdate(Player player, float delta) {
    if (player.IsOnFloor()) {
      EventHandler.Instance.EmitPlayerLand();
      return statesStore.GetState<PlayerStandingState>();
    }
    if (JumpPressed(player) && permissivenessTimer.IsRunning()) {
      permissivenessTimer.Stop();
      return OnJump(player);
    }
    permissivenessTimer.Step(delta);
    return null;
  }

  public IState<Player>? OnAnimationFinished(string animName) {
    return null;
  }
}
