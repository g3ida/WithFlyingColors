namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.State;
using Wfc.Utils;

public partial class PlayerJumpingState : PlayerBaseState {
  private const float TIME_UNTIL_FULL_JUMP_IS_CONSIDERED = 0.15f;
  private const float PERMISSIVENESS = 0.09f;
  private const float FACE_SEPARATOR_SCALE_FACTOR = 4.5f;
  private const float JUMP_FORCE = 1200f;
  private const float JUMP_CUT_FACTOR = 0.5f;

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
    GameEvents.Instance.OnPlayerJumped();
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
        GameEvents.Instance.OnPlayerLanded();
        return statesStore.GetState<PlayerStandingState>();
      }
    }

    if (JumpPressed(player)) {
      _permissivenessTimer.Reset();
    }

    if (_jumpTimer.IsRunning() && inputManager.IsJustReleased(IInputManager.Action.Jump)) {
      _jumpTimer.Stop();
      if (player.Velocity.Y < 0) {
        player.Velocity = ApplyJumpCut(player.Velocity, JUMP_CUT_FACTOR, player.CarriedVelocity.Y);
      }
    }

    _jumpTimer.Step(delta);
    _permissivenessTimer.Step(delta);
    return null;
  }

  // Releasing Jump early cuts the jump short, so it is the rising component that gets
  // damped. Damping X instead gave every tap-jump full height and half run speed, which
  // shortened the arc without ever letting the player duck under a low ceiling.
  //
  // The cut is over how high the cube jumps, so a lift handed to it by the floor it jumped off is
  // left whole - and it can never leave the cube rising faster than the jump it is cutting.
  //
  // Only a lift that lifts. A floor that was sinking is left out of the carry by the cube's
  // platform_on_leave, which is a scene property and no business of the cut's: a downward one
  // reaching here would be cut for more than the jump is worth rather than less.
  internal static Vector2 ApplyJumpCut(Vector2 velocity, float factor, float carriedLift) {
    var lift = Mathf.Min(carriedLift, 0.0f);
    return new(velocity.X, Mathf.Max(((velocity.Y - lift) * factor) + lift, velocity.Y));
  }

  public PlayerJumpingState WithJumpPower(float jumpPower) {
    _touchJumpPower = jumpPower;
    return this;
  }
}
