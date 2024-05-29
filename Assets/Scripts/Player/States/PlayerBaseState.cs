using Godot;
using System;

public class PlayerBaseStateCS : BaseStateCS<Player>
{
    public PlayerStatesEnum baseState;

    const float GRAVITY = 9.8f * Constants.WORLD_TO_SCREEN;
    const float FALL_FACTOR = 2.5f;

    public PlayerBaseStateCS() {}

    public bool playerMoved = false;

    public sealed override void Enter(Player player)
    {
        ResetTouchInput(player);
        player.ScaleCornersBy(player.CurrentDefaultCornerScaleFactor);
        _Enter(player);
    }

    public sealed override void Exit(Player player)
    {
        ResetTouchInput(player);
        _Exit(player);
    }

    protected virtual void _Enter(Player player) { }
    protected virtual void _Exit(Player player) { }

    public sealed override void _Input(Player player, InputEvent @event)
    {
        if (@event is InputTouchMove)
        {
            player.touch_move_input = @event as InputTouchMove;
        }
        else if (@event is InputTouchJump)
        {
            player.touch_jump_input = @event as InputTouchJump;
        }
        else if (@event is InputTouchDash)
        {
            player.touch_dash_input = @event as InputTouchDash;
        }
        else if (@event is InputTouchRotate)
        {
            player.touch_rotation_input = @event as InputTouchRotate;
        }
        input(player, @event);
    }

    // FIXME: rename to uppercase after c# migration so I don't clash with input class.
    protected virtual void input(Player player, InputEvent @event) { }

    protected bool DashActionPressed(Player player)
    {
        return (Input.IsActionJustPressed("dash") || player.touch_dash_input != null) && player.can_dash && !player.handle_input_is_disabled;
    }

    public override BaseStateCS<Player> PhysicsUpdate(Player player, float delta)
    {
        if (player.player_state != player.states_store.GetState(PlayerStatesEnum.DYING))
        {
            if (DashActionPressed(player))
            {
                return OnDash(player);
            }

            if (!player.handle_input_is_disabled)
            {
                if (player.player_state != player.states_store.GetState(PlayerStatesEnum.DASHING))
                {
                    if (Input.IsActionPressed("move_right"))
                    {
                        playerMoved = true;
                        player.velocity.x = Mathf.Clamp(player.velocity.x + player.speed_unit, 0, player.speed_limit);
                    }
                    else if (Input.IsActionPressed("move_left"))
                    {
                        playerMoved = true;
                        player.velocity.x = Mathf.Clamp(player.velocity.x - player.speed_unit, -player.speed_limit, 0);
                    }
                    else if (player.touch_move_input != null)
                    {
                        playerMoved = true;
                        float minV = Mathf.Min(Mathf.Sign(player.touch_move_input.Direction.x) * player.speed_limit, 0);
                        float maxV = Mathf.Max(Mathf.Sign(player.touch_move_input.Direction.x) * player.speed_limit, 0);
                        player.velocity.x = Mathf.Clamp(player.velocity.x + player.touch_move_input.Direction.x * player.speed_unit, minV, maxV);
                    }
                }
            }

            player.velocity.y += GRAVITY * delta * FALL_FACTOR;
        }

        var newState = _PhysicsUpdate(player, delta);

        player.velocity = player.MoveAndSlide(player.velocity, Vector2.Up);
        player.velocity.x = Mathf.Lerp(player.velocity.x, 0, 0.25f);
        player.current_animation.Step(player, player.animatedSpriteNode, delta);

        if (newState != null)
        {
            return newState;
        }
        else
        {
            ResetTouchInput(player);
            return null;
        }
    }

    protected void ResetTouchInput(Player player)
    {
        player.touch_jump_input = null;
        player.touch_dash_input = null;
        player.touch_rotation_input = null;
    }

    protected virtual BaseStateCS<Player> _PhysicsUpdate(Player player, float delta) { return null; }

    protected void SetPlayerDeathAnimationType(PlayerDyingState dyingState, Constants.EntityType entityType)
    {
        if (entityType == Constants.EntityType.FALLZONE)
        {
            dyingState.deathAnimationType = DeathAnimationType.DYING_FALL;
        }
        else
        {
            dyingState.deathAnimationType = DeathAnimationType.DYING_EXPLOSION_REAL;
        }
    }

    public PlayerBaseStateCS OnPlayerDying(Player player, Node area, Vector2 position, Constants.EntityType entityType)
    {
        if (entityType != Constants.EntityType.FALLZONE)
        {
            player.velocity = Vector2.Zero;
        }

        var dyingState = (PlayerDyingState)player.states_store.GetState(PlayerStatesEnum.DYING);
        SetPlayerDeathAnimationType(dyingState, entityType);
        return dyingState;
    }

    public PlayerBaseStateCS OnLand(Player player)
    {
        player.current_animation = player.scale_animation;
        if (!player.current_animation.IsRunning())
        {
            player.current_animation.Start();
        }
        return null;
    }

    protected BaseStateCS<Player> OnDash(Player player)
    {
        var dashingState = (PlayerDashingState)player.states_store.GetState(PlayerStatesEnum.DASHING);
        if (player.touch_dash_input != null)
        {
            dashingState.direction = player.touch_dash_input.Direction;
        }
        return dashingState;
    }

    protected BaseStateCS<Player> OnJump(Player player)
    {
        var jumpState = player.states_store.GetState(PlayerStatesEnum.JUMPING);
        if (player.touch_jump_input != null)
        {
            // FIXME: make this look better after c# migration
            jumpState.Set("touchJumpPower", player.touch_jump_input.Force);
        }
        return jumpState;
    }

    protected bool JumpPressed(Player player)
    {
        if (player.handle_input_is_disabled) return false;
        return Input.IsActionJustPressed("jump") || player.touch_jump_input != null;
    }

    protected BaseStateCS<Player> HandleRotate(Player player)
    {
        if (player.player_state.baseState != PlayerStatesEnum.DYING && !player.handle_input_is_disabled)
        {
            if (Input.IsActionJustPressed("rotate_left"))
            {
                return player.states_store.GetState(PlayerStatesEnum.ROTATING_LEFT);
            }
            if (Input.IsActionJustPressed("rotate_right"))
            {
                return player.states_store.GetState(PlayerStatesEnum.ROTATING_RIGHT);
            }
            // touch
            if (player.touch_rotation_input != null)
            {
                if (player.touch_rotation_input.Direction > 0)
                {
                    return player.states_store.GetState(PlayerStatesEnum.ROTATING_RIGHT);
                }
                else
                {
                    return player.states_store.GetState(PlayerStatesEnum.ROTATING_LEFT);
                }
            }
        }
        return null;
    }
}
