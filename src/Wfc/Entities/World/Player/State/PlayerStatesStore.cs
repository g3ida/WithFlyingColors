namespace Wfc.Entities.World.Player;

using System;
using Godot;
using Wfc.State;

public partial class PlayerStatesStore : BaseStatesStore<Player, PlayerStatesEnum> {
  // Player states
  public PlayerStandingState standingState;
  public PlayerJumpingState jumpingState;
  public PlayerFallingState fallingState;
  public PlayerDyingState dyingState;
  public PlayerDashingState dashingState;
  public PlayerSlipperingState slipperingState;

  // Rotation states
  public PlayerRotatingState rotatingRightState;
  public PlayerRotatingState rotatingLeftState;
  public PlayerRotatingIdleState idleState;

  public PlayerStatesStore(Player player) {

    standingState = new PlayerStandingState();
    jumpingState = new PlayerJumpingState();
    fallingState = new PlayerFallingState();
    dyingState = new PlayerDyingState();
    dashingState = new PlayerDashingState();

    rotatingRightState = new PlayerRotatingState();
    rotatingRightState.Init(player, 1);
    rotatingLeftState = new PlayerRotatingState();
    rotatingLeftState.Init(player, -1);

    idleState = new PlayerRotatingIdleState();

    slipperingState = new PlayerSlipperingState();
  }

  public override BaseState<Player>? GetState(PlayerStatesEnum state) {
    switch (state) {
      case PlayerStatesEnum.IDLE:
        return idleState;
      case PlayerStatesEnum.ROTATING_LEFT:
        return rotatingLeftState;
      case PlayerStatesEnum.ROTATING_RIGHT:
        return rotatingRightState;
      case PlayerStatesEnum.STANDING:
        return standingState;
      case PlayerStatesEnum.JUMPING:
        return jumpingState;
      case PlayerStatesEnum.FALLING:
        return fallingState;
      case PlayerStatesEnum.DYING:
        return dyingState;
      case PlayerStatesEnum.DASHING:
        return dashingState;
      case PlayerStatesEnum.SLIPPERING:
        return slipperingState;
      default:
        return null;
    }
  }
}
