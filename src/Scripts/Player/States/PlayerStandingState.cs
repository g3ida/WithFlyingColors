using Godot;
using System;

public partial class PlayerStandingState : PlayerBaseState
{
  private const float RAYCAST_LENGTH = 10.0f;
  private const float RAYCAST_Y_OFFSET = -5.0f; // https://godotengine.org/qa/63336/raycast2d-doesnt-collide-with-tilemap
  private const float SLIPPERING_LIMIT = 0.42f; // higher is less slippering

  public PlayerStandingState() : base()
  {
    baseState = PlayerStatesEnum.STANDING;
  }

  protected override void _Enter(Player player)
  {
    player.animatedSpriteNode.Play("idle");
    player.animatedSpriteNode.Stop();
    Event.Instance.EmitPlayerLand();
    player.can_dash = true;
  }

  protected override void _Exit(Player player)
  {
  }

  public override BaseState<Player> PhysicsUpdate(Player player, float delta)
  {
    return base.PhysicsUpdate(player, delta);
  }

  protected override BaseState<Player> _PhysicsUpdate(Player player, float delta)
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
      if (Math.Abs(player.Velocity.X) < player.speed_unit
          && player.player_rotation_state.baseState == PlayerStatesEnum.IDLE)
      {
        return RaycastFloor(player);
      }
    }
    return null;
  }

  private BaseState<Player> RaycastFloor(Player player)
  {
    var spaceState = player.GetWorld2D().DirectSpaceState;
    var playerHalfSize = player.GetCollisionShapeSize() * 0.5f * player.Scale;

    int combination = 0;
    int i = 1;
    float[] fromOffsetX = {
            -playerHalfSize.X,
            -playerHalfSize.X * SLIPPERING_LIMIT,
            playerHalfSize.X * SLIPPERING_LIMIT,
            playerHalfSize.X
        };

    foreach (var offset in fromOffsetX)
    {
      Vector2 from = player.GlobalPosition + new Vector2(offset, playerHalfSize.Y + RAYCAST_Y_OFFSET);
      Vector2 to = from + new Vector2(0.0f, RAYCAST_LENGTH);
      var physicsRayQueryParameters = PhysicsRayQueryParameters2D.Create(
          from, to, exclude: new Godot.Collections.Array<Rid> { player.GetRid() }
      );

      var result = spaceState.IntersectRay(physicsRayQueryParameters);
      if (result.ContainsKey("collider"))
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
