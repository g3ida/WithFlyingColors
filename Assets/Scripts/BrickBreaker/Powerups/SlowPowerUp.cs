using Godot;
using System;

public class SlowPowerUp : PowerUpScript
{
    public override void _EnterTree()
    {
        SetProcess(false);
        var player = Global.Instance().Player;
        player.speed_limit = 0.5f * Player.SPEED;
        player.speed_unit = 0.5f * Player.SPEED_UNIT;
    }

    public override void _ExitTree()
    {
        if (IsStillRelevant())
        {
            var player = Global.Instance().Player;
            player.speed_limit = Player.SPEED;
            player.speed_unit = Player.SPEED_UNIT;
        }
    }

    public override void _Ready()
    {
    }

    public override bool IsStillRelevant()
    {
        var player = Global.Instance().Player;
        return Mathf.Abs(player.speed_limit - 0.5f * Player.SPEED) < Constants.EPSILON;
    }
}
