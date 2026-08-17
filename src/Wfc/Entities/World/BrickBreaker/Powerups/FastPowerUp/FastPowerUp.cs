namespace Wfc.Entities.World.BrickBreaker.Powerups;

using System;
using Godot;
using Wfc.Entities.World.Player;
using Wfc.Screens.Levels;
using Wfc.Utils;

public partial class FastPowerUp : PowerUpScript {
  public override void _EnterTree() {
    SetProcess(false);
    if (GameRepo.Instance.Player.Value is not { } player) {
      return;
    }
    player.SpeedLimit = 2.0f * Player.SPEED;
    player.SpeedUnit = 2.0f * Player.SPEED_UNIT;
  }

  public override void _ExitTree() {
    // The level can come down around a powerup that is still up, taking the cube with it -
    // there is then nothing left to restore.
    if (GameRepo.Instance.Player.Value is { } player && _isApplied(player)) {
      player.SpeedLimit = Player.SPEED;
      player.SpeedUnit = Player.SPEED_UNIT;
    }
  }

  public override void _Ready() {
  }

  public override bool IsStillRelevant() =>
    GameRepo.Instance.Player.Value is { } player && _isApplied(player);

  private static bool _isApplied(Player player) =>
    Mathf.Abs(player.SpeedLimit - 2.0f * Player.SPEED) < MathUtils.EPSILON;
}
