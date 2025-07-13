namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Core.Input;
using Wfc.State;
using EventHandler = Wfc.Core.Event.EventHandler;

public abstract partial class PlayerRotatingState : PlayerRotatingIdleState {
  protected abstract int rotationDirection { get; }

  public PlayerRotatingState(IPlayerStatesStore statesStore, IInputManager inputManager)
    : base(statesStore, inputManager) {
  }

  public override void Enter(Player player) {
    base.Enter(player);
    bool cumulateAngle = !player.IsSlippering();
    player.PlayerRotationAction.Execute(rotationDirection, Constants.PI2, 0.1f, true, cumulateAngle, true);
    EventHandler.Instance.EmitPlayerRotate(rotationDirection);
  }

  public override IState<Player>? PhysicsUpdate(Player player, float delta) {
    var baseResult = base.PhysicsUpdate(player, delta);
    if (player.PlayerRotationAction.CanRotate) {
      return baseResult ?? statesStore.GetState<PlayerRotatingIdleState>();
    }
    return baseResult;
  }
}
