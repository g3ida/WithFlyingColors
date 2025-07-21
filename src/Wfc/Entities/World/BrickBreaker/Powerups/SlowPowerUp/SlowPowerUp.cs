namespace Wfc.Entities.World.BrickBreaker.Powerups;

using System;
using Godot;
using Wfc.Entities.World.Player;
using Wfc.Utils;

public partial class SlowPowerUp : PowerUpScript {
  public override void _EnterTree() {
    base._EnterTree();
    SetProcess(false);
    var player = Global.Instance().Player;
    player.SpeedLimit = 0.5f * Player.SPEED;
    player.SpeedUnit = 0.5f * Player.SPEED_UNIT;
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (IsStillRelevant()) {
      var player = Global.Instance().Player;
      player.SpeedLimit = Player.SPEED;
      player.SpeedUnit = Player.SPEED_UNIT;
    }
  }

  public override void _Ready() {
    base._Ready();
  }

  public override bool IsStillRelevant() {
    var player = Global.Instance().Player;
    return Mathf.Abs(player.SpeedLimit - 0.5f * Player.SPEED) < MathUtils.EPSILON;
  }
}
