using Godot;
using System;

public partial class PlayerSlipperingState : PlayerBaseState
{
    public int direction = 1;
    private PlayerRotationAction playerRotation;

    private const float RAYCAST_Y_OFFSET = -3.0f;
    private const float RAYCAST_LENGTH = 5.0f;
    private const float CORRECT_ROTATION_FALL_SPEED = 0.3f;
    private const float CORRECT_ROTATION_JUMP_SPEED = 0.07f;
    private const float PLAYER_SPEED_THRESHOLD_TO_STAND = -300.0f;
    private const float SLIPPERING_ROTATION_DURATION = 3.0f;
    private const float SLIPPERING_RECOVERY_INITIAL_DURATION = 0.8f;

    private float exitRotationSpeed = CORRECT_ROTATION_JUMP_SPEED;
    private bool skipExitRotation = false;

    public PlayerSlipperingState() : base()
    {
        this.baseState = PlayerStatesEnum.SLIPPERING;
    }

    protected override void _Enter(Player player)
    {
        playerRotation = player.playerRotationAction;
        player.animatedSpriteNode.Play("idle");
        player.animatedSpriteNode.Stop();
        skipExitRotation = false;
        exitRotationSpeed = CORRECT_ROTATION_JUMP_SPEED;
        playerRotation.Execute(direction, Constants.PI2, SLIPPERING_ROTATION_DURATION, true, false, true);
        Event.Instance().EmitPlayerSlippering();
        player.can_dash = true;
    }

    protected override void _Exit(Player player)
    {
        if (!skipExitRotation)
        {
            playerRotation.Execute(-direction, Constants.PI2, SLIPPERING_RECOVERY_INITIAL_DURATION, true, false, false);
            player.GetTree().CreateTimer(0.05f).Connect("timeout", new Callable(this, nameof(OnExitTimerTimeout)));
        }
    }

    private void OnExitTimerTimeout()
    {
        playerRotation.Execute(-direction, Constants.PI2, exitRotationSpeed, true, false, false);
    }

    public override BaseState<Player> PhysicsUpdate(Player player, float delta)
    {
        return base.PhysicsUpdate(player, delta);
    }

    protected override BaseState<Player> _PhysicsUpdate(Player player, float delta)
    {
        if (JumpPressed(player) && player.IsOnFloor())
        {
            exitRotationSpeed = CORRECT_ROTATION_JUMP_SPEED;
            return OnJump(player);
        }

        if (!player.IsOnFloor())
        {
            var fallingState = (PlayerFallingState)player.states_store.GetState(PlayerStatesEnum.FALLING);
            if (Mathf.Abs(player.Rotation - playerRotation.thetaZero) > Constants.PI8 && !CheckIfGroundIsNear(player))
            {
                exitRotationSpeed = CORRECT_ROTATION_FALL_SPEED;
                fallingState.wasOnFloor = true;
                direction = -direction;
            }
            return fallingState;
        }

        if (player.player_rotation_state.baseState != PlayerStatesEnum.IDLE)
        {
            skipExitRotation = true;
            return player.states_store.GetState(PlayerStatesEnum.STANDING);
        }

        if (playerRotation.canRotate || playerMoved)
        {
            return player.states_store.GetState(PlayerStatesEnum.STANDING);
        }

        return HandleGroundIsNear(player);
    }

    private Vector2 GetFallingEdgePosition(Player player)
    {
        var corners = new CollisionShape2D[] { player.FaceCollisionShapeTL_node, player.FaceCollisionShapeTR_node,
                                      player.FaceCollisionShapeBL_node, player.FaceCollisionShapeBR_node };

        var pp = player.GlobalPosition;
        var position = pp;
        var size = player.GetCollisionShapeSize() * 0.5f * player.Scale;

        foreach (var cc in corners)
        {
            var cp = cc.GlobalPosition;
            if (Mathf.Sign(pp.X - cp.X) == -direction && cp.Y > position.Y)
            {
                position = cp;
                size = (cc.Shape as RectangleShape2D).Size;
            }
        }

        return position + new Vector2(-0.5f * direction * size.X, 0.5f * size.Y) * player.Scale;
    }

    private bool CheckIfGroundIsNear(Player player)
    {
        var spaceState = player.GetWorld2D().DirectSpaceState;
        var from = GetFallingEdgePosition(player) + Vector2.Up * RAYCAST_Y_OFFSET;
        var to = from + new Vector2(0.0f, RAYCAST_LENGTH);
        var physicsRayQueryParameters = PhysicsRayQueryParameters2D.Create(
            from, to, exclude: new Godot.Collections.Array<Rid> { player.GetRid() }
        );
        var result = spaceState.IntersectRay(physicsRayQueryParameters);

        return result.Count != 0;
    }

    private BaseState<Player> HandleGroundIsNear(Player player)
    {
        if (CheckIfGroundIsNear(player))
        {
            player.velocity.X -= player.Scale.X * direction * PLAYER_SPEED_THRESHOLD_TO_STAND;
            return player.states_store.GetState(PlayerStatesEnum.STANDING);
        }
        return null;
    }
}
