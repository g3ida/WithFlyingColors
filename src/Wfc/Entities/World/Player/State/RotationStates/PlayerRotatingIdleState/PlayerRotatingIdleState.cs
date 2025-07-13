namespace Wfc.Entities.World.Player;

using System;
using Godot;
using Wfc.Core.Input;
using Wfc.State;

public partial class PlayerRotatingIdleState : IState<Player> {

  protected IPlayerStatesStore statesStore;
  protected IInputManager inputManager;

  public PlayerRotatingIdleState(IPlayerStatesStore statesStore, IInputManager inputManager) {
    this.statesStore = statesStore;
    this.inputManager = inputManager;
  }

  public virtual void Enter(Player player) { }

  public virtual void Exit(Player player) { }

  public virtual IState<Player>? PhysicsUpdate(Player player, float delta) {
    player.PlayerRotationAction.Step(delta);
    return _handleRotate(player);
  }

  private IState<Player>? _handleRotate(Player player) {
    if (!player.IsDying() && !player.HandleInputIsDisabled) {
      if (inputManager.IsJustPressed(IInputManager.Action.RotateLeft)) {
        return statesStore.GetState<PlayerRotatingLeftState>();
      }
      if (inputManager.IsJustPressed(IInputManager.Action.RotateRight)) {
        return statesStore.GetState<PlayerRotatingRightState>();
      }
    }
    return null;
  }

  public void Init(Player o) { }
}
