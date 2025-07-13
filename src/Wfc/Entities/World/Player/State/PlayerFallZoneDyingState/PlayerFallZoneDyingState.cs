namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Core.Input;
using Wfc.Entities.World.Explosion;
using Wfc.State;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class PlayerFallZoneDyingState : PlayerBaseState {
  public PlayerFallZoneDyingState(IPlayerStatesStore statesStore, IInputManager inputManager)
    : base(statesStore, inputManager) {
  }

  protected override void _Enter(Player player) {
    player.HideColorAreas();
    player.SetCollisionShapesDisabledFlagDeferred(true);
    EventHandler.Instance.EmitPlayerFall();
    player.FallTimerNode.Start();
    player.FallTimerNode.Timeout += OnFallTimeout;
  }

  private void OnFallTimeout() {
    EventHandler.Instance.EmitPlayerDied();
  }

  protected override void _Exit(Player player) {
    player.FallTimerNode.Timeout -= OnFallTimeout;
    player.ShowColorAreas();
    player.SetCollisionShapesDisabledFlagDeferred(false);
  }
}
