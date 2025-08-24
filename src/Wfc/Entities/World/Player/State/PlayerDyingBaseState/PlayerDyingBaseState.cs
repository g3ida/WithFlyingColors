namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Core.Input;
using Wfc.Entities.World.Explosion;
using Wfc.State;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class PlayerDyingBaseState : PlayerBaseState {
  public PlayerDyingBaseState(IPlayerStatesStore statesStore, IInputManager inputManager)
    : base(statesStore, inputManager) {
  }

  protected override void _Enter(Player player) {
    player.HideColorAreas();
    player.SetCollisionShapesDisabledFlagDeferred(true);
  }

  protected override void _Exit(Player player) {
    player.ShowColorAreas();
    player.SetCollisionShapesDisabledFlagDeferred(false);
  }
}
