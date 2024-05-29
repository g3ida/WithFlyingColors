using Godot;
using System;

public class PlayerStandingState : PlayerBaseStateCS
{
    private const float RAYCAST_LENGTH = 10.0f;
    private const float RAYCAST_Y_OFFSET = -3.0f; // https://godotengine.org/qa/63336/raycast2d-doesnt-collide-with-tilemap
    private const float SLIPPERING_LIMIT = 0.42f; // higher is less slippering

    public PlayerStandingState() : base()
    {
        baseState = PlayerStatesEnum.STANDING;
    }

    protected override void _Enter(Player player)
    {
        player.animatedSpriteNode.Play("idle");
        player.animatedSpriteNode.Playing = false;
        Event.Instance().EmitPlayerLand();
        player.can_dash = true;
    }

    protected override void _Exit(Player player)
    {
    }

    public override BaseStateCS<Player> PhysicsUpdate(Player player, float delta)
    {
        return base.PhysicsUpdate(player, delta);
    }

    protected override BaseStateCS<Player> _PhysicsUpdate(Player player, float delta)
    {
        if (JumpPressed(player) && player.IsOnFloor())
        {
            return OnJump(player);
        }
        if (!player.IsOnFloor())
        {
            var fallingState = (PlayerFallingState)player.states_store.GetState(PlayerStatesEnum.FALLING) as PlayerFallingState;
            fallingState.wasOnFloor = true;
            return fallingState;
        }
        else
        {
            if (Math.Abs(player.velocity.x) < player.speed_unit
                && player.player_rotation_state.baseState == PlayerStatesEnum.IDLE)
            {
                return RaycastFloor(player);
            }
        }
        return null;
    }

    private BaseStateCS<Player> RaycastFloor(Player player)
    {
        var spaceState = player.GetWorld2d().DirectSpaceState;
        var playerHalfSize = player.GetCollisionShapeSize() * 0.5f * player.Scale;

        int combination = 0;
        int i = 1;
        float[] fromOffsetX = {
            -playerHalfSize.x,
            -playerHalfSize.x * SLIPPERING_LIMIT,
            playerHalfSize.x * SLIPPERING_LIMIT,
            playerHalfSize.x
        };

        foreach (var offset in fromOffsetX)
        {
            Vector2 from = player.GlobalPosition + new Vector2(offset, playerHalfSize.y + RAYCAST_Y_OFFSET);
            Vector2 to = from + new Vector2(0.0f, RAYCAST_LENGTH);
            var result = spaceState.IntersectRay(from, to, new Godot.Collections.Array { player });
            if (result.Count != 0)
            {
                combination += i;
            }
            i *= 2;
        }

        if (combination == 1 || combination == 8) // flag values
        {
            var slipperingState = player.states_store.GetState(PlayerStatesEnum.SLIPPERING) as PlayerSlipperingState;
            slipperingState.direction = combination == 1 ? 1 : -1;
            return slipperingState;
        }
        return null;
    }
}
