namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.State;
using Wfc.Utils;

public partial class PlayerFallingState : PlayerBaseState {
  public bool WasOnFloor = false;
  private const float PERMISSIVENESS = 0.04f;
  private CountdownTimer _permissivenessTimer = new CountdownTimer();

  public PlayerFallingState(IPlayerStatesStore statesStore, IInputManager inputManager)
    : base(statesStore, inputManager) {
    _permissivenessTimer.Set(PERMISSIVENESS, false);
  }

  protected override void _Enter(Player player) {
    player.AnimatedSpriteNode.Play("idle");
    player.AnimatedSpriteNode.Stop();
    if (WasOnFloor) {
      _permissivenessTimer.Reset();
    }
    player.JumpParticlesNode.Emitting = true;
  }

  protected override void _Exit(Player player) {
    _permissivenessTimer.Stop();
    player.JumpParticlesNode.Emitting = false;
    WasOnFloor = false;
  }

  protected override IState<Player>? _PhysicsUpdate(Player player, float delta) {
    if (player.IsOnFloor()) {
      EventHandler.Instance.EmitPlayerLand();
      return statesStore.GetState<PlayerStandingState>();
    }
    if (JumpPressed(player) && _permissivenessTimer.IsRunning()) {
      _permissivenessTimer.Stop();
      return OnJump(player);
    }
    _permissivenessTimer.Step(delta);
    return null;
  }

  public IState<Player>? OnAnimationFinished(string animName) {
    return null;
  }
}
