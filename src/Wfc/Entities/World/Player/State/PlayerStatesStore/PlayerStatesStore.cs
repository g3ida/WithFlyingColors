namespace Wfc.Entities.World.Player;

using System;
using Godot;
using Wfc.Core.Input;
using Wfc.State;

public partial class PlayerStatesStore : GodotObject, IPlayerStatesStore {
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

  public PlayerStatesStore(IInputManager inputManager) {
    standingState = new PlayerStandingState(this, inputManager);
    jumpingState = new PlayerJumpingState(this, inputManager);
    fallingState = new PlayerFallingState(this, inputManager);
    dyingState = new PlayerDyingState(this, inputManager);
    dashingState = new PlayerDashingState(this, inputManager);
    slipperingState = new PlayerSlipperingState(this, inputManager);
    // rotation states
    rotatingRightState = new PlayerRotatingState(this, 1, inputManager);
    rotatingLeftState = new PlayerRotatingState(this, -1, inputManager);
    idleState = new PlayerRotatingIdleState(this, inputManager);
  }

  public IState<Player>? GetState(PlayerStatesEnum state) {
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
