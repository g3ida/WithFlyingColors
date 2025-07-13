namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Core.Input;
using Wfc.State;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class PlayerJumpingState : PlayerBaseState {
  private const float TIME_UNTIL_FULL_JUMP_IS_CONSIDERED = 0.15f;
  private const float PERMISSIVENESS = 0.09f;
  private const float FACE_SEPARATOR_SCALE_FACTOR = 4.5f;
  private const float JUMP_FORCE = 1200f;

  private bool _entered = false;
  private CountdownTimer _jumpTimer = new CountdownTimer();
  private CountdownTimer _permissivenessTimer = new CountdownTimer();
  private float _touchJumpPower = 1.0f;

  public PlayerJumpingState(IPlayerStatesStore statesStore, IInputManager inputManager)
    : base(statesStore, inputManager) {
    _jumpTimer.Set(TIME_UNTIL_FULL_JUMP_IS_CONSIDERED, false);
    _permissivenessTimer.Set(PERMISSIVENESS, false);
  }

  protected override void _Enter(Player player) {
    _entered = true;
    _jumpTimer.Reset();
    EventHandler.Instance.EmitPlayerJumped();
    player.JumpParticlesNode.Emitting = true;
    player.ScaleCornersBy(FACE_SEPARATOR_SCALE_FACTOR);
  }

  protected override void _Exit(Player player) {
    _entered = false;
    _jumpTimer.Stop();
    _permissivenessTimer.Stop();
    player.JumpParticlesNode.Emitting = false;
    player.ScaleCornersBy(1);
    _touchJumpPower = 1.0f;
  }

  protected override IState<Player>? _PhysicsUpdate(Player player, float delta) {
    if (_entered) {
      _entered = false;
      player.Velocity = new Vector2(player.Velocity.X, player.Velocity.Y - JUMP_FORCE * _touchJumpPower);
    }
    else if (player.IsOnFloor()) {
      if (_permissivenessTimer.IsRunning()) {
        return statesStore.GetState<PlayerJumpingState>();
      }
      else {
        EventHandler.Instance.EmitPlayerLand();
        return statesStore.GetState<PlayerStandingState>();
      }
    }

    if (JumpPressed(player)) {
      _permissivenessTimer.Reset();
    }

    if (_jumpTimer.IsRunning() && inputManager.IsJustReleased(IInputManager.Action.Jump)) {
      _jumpTimer.Stop();
      if (player.Velocity.Y < 0) {
        player.Velocity = new Vector2(player.Velocity.X * 0.5f, player.Velocity.Y);
      }
    }

    _jumpTimer.Step(delta);
    _permissivenessTimer.Step(delta);
    return null;
  }

  public PlayerJumpingState WithJumpPower(float jumpPower) {
    _touchJumpPower = jumpPower;
    return this;
  }
}
