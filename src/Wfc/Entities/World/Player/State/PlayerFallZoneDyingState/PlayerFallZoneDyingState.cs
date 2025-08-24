namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Core.Input;
using Wfc.Entities.World.Explosion;
using Wfc.State;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class PlayerFallZoneDyingState : PlayerDyingBaseState {
  public PlayerFallZoneDyingState(IPlayerStatesStore statesStore, IInputManager inputManager)
    : base(statesStore, inputManager) {
  }

  protected override void _Enter(Player player) {
    base._Enter(player);
    EventHandler.Instance.EmitPlayerFall();
    player.FallTimerNode.Start();
    player.FallTimerNode.Timeout += OnFallTimeout;
  }

  private void OnFallTimeout() {
    EventHandler.Instance.EmitPlayerDied();
  }

  protected override void _Exit(Player player) {
    base._Exit(player);
    player.FallTimerNode.Timeout -= OnFallTimeout;
  }
}
