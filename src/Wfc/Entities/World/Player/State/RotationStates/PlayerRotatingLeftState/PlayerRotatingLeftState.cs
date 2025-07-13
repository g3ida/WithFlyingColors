namespace Wfc.Entities.World.Player;

using Wfc.Core.Input;

public class PlayerRotatingLeftState : PlayerRotatingState {
  protected override int rotationDirection => -1;

  public PlayerRotatingLeftState(IPlayerStatesStore statesStore, IInputManager inputManager)
      : base(statesStore, inputManager) {
  }
}
