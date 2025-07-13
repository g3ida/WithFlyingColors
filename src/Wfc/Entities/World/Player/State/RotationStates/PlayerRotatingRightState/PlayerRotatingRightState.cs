namespace Wfc.Entities.World.Player;

using Wfc.Core.Input;

public class PlayerRotatingRightState : PlayerRotatingState {
  protected override int rotationDirection => 1;

  public PlayerRotatingRightState(IPlayerStatesStore statesStore, IInputManager inputManager)
      : base(statesStore, inputManager) {
  }
}
