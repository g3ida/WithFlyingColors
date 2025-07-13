namespace Wfc.Entities.World.Player;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Core.Input;
using Wfc.State;

public partial class PlayerStatesStore : GodotObject, IPlayerStatesStore {

  private readonly Dictionary<Type, IState<Player>> _states;

  public PlayerStatesStore(IInputManager inputManager) {
    _states = new Dictionary<Type, IState<Player>> {
            { typeof(PlayerRotatingIdleState), new PlayerRotatingIdleState(this, inputManager) },
            { typeof(PlayerRotatingLeftState), new PlayerRotatingLeftState(this, inputManager) },
            { typeof(PlayerRotatingRightState), new PlayerRotatingRightState(this, inputManager) },
            { typeof(PlayerStandingState), new PlayerStandingState(this, inputManager)},
            { typeof(PlayerJumpingState), new PlayerJumpingState(this, inputManager) },
            { typeof(PlayerFallingState), new PlayerFallingState(this, inputManager) },
            { typeof(PlayerFallZoneDyingState), new PlayerFallZoneDyingState(this, inputManager) },
            { typeof(PlayerExplosionState), new PlayerExplosionState(this, inputManager) },
            { typeof(PlayerDashingState), new PlayerDashingState(this, inputManager) },
            { typeof(PlayerSlipperingState), new PlayerSlipperingState(this, inputManager) }
        };
  }

  public TState? GetState<TState>() where TState : class, IState<Player> {
    if (_states.TryGetValue(typeof(TState), out var state)) {
      return state as TState;
    }
    return null;
  }
}
