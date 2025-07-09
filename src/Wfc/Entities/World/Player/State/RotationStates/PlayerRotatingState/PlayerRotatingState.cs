namespace Wfc.Entities.World.Player;

using Wfc.State;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class PlayerRotatingState : PlayerBaseState {
  private int rotationDirection = 0;

  public PlayerRotatingState() : base() { }

  public void Init(Player player, int direction) {
    rotationDirection = direction;
    this.baseState = direction == -1 ? PlayerStatesEnum.ROTATING_LEFT : PlayerStatesEnum.ROTATING_RIGHT;
  }

  protected override void _Enter(Player player) {
    bool cumulateAngle = player.PlayerState.baseState != PlayerStatesEnum.SLIPPERING;
    player.PlayerRotationAction.Execute(rotationDirection, Constants.PI2, 0.1f, true, cumulateAngle, true);
    EventHandler.Instance.EmitPlayerRotate(rotationDirection);
  }

  public override BaseState<Player>? PhysicsUpdate(Player player, float delta) {
    player.PlayerRotationAction.Step(delta);
    if (player.PlayerRotationAction.CanRotate) {
      return player.StatesStore.GetState(PlayerStatesEnum.IDLE);
    }
    return HandleRotate(player);
  }
}
