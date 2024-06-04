using Godot;
using System;

public partial class PlayerBaseState : BaseState<Player>
{
    public PlayerStatesEnum baseState;

    const float GRAVITY = 9.8f * Constants.WORLD_TO_SCREEN;
    const float FALL_FACTOR = 2.5f;

    public PlayerBaseState() {}

    public bool playerMoved = false;

    public sealed override void Enter(Player player)
    {
        player.ScaleCornersBy(player.CurrentDefaultCornerScaleFactor);
        _Enter(player);
    }

    public sealed override void Exit(Player player)
    {
        _Exit(player);
    }

    protected virtual void _Enter(Player player) { }
    protected virtual void _Exit(Player player) { }

    public sealed override void _Input(Player player, InputEvent ev)
    {
        input(player, ev);
    }

    // FIXME: rename to uppercase after c# migration so I don't clash with input class.
    protected virtual void input(Player player, InputEvent ev) { }

    protected bool DashActionPressed(Player player)
    {
        return Input.IsActionJustPressed("dash") && player.can_dash && !player.handle_input_is_disabled;
    }

    public override BaseState<Player> PhysicsUpdate(Player player, float delta)
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
                        player.velocity.X = Mathf.Clamp(player.velocity.X + player.speed_unit, 0, player.speed_limit);
                    }
                    else if (Input.IsActionPressed("move_left"))
                    {
                        playerMoved = true;
                        player.velocity.X = Mathf.Clamp(player.velocity.X - player.speed_unit, -player.speed_limit, 0);
                    }
                }
            }

            player.velocity.Y += GRAVITY * delta * FALL_FACTOR;
        }

        var newState = _PhysicsUpdate(player, delta);

        // FIXME: cleanup this logic after c# migration
        player.Velocity = player.velocity; // set the velocity to the player
        player.MoveAndSlide();
        player.velocity = player.Velocity; // get the velocity forom player
        player.velocity.X = Mathf.Lerp(player.velocity.X, 0, 0.25f);
        player.current_animation.Step(player, player.animatedSpriteNode, delta);

        if (newState != null)
        {
            return newState;
        }
        else
        {
            return null;
        }
    }

    protected virtual BaseState<Player> _PhysicsUpdate(Player player, float delta) { return null; }

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

    public PlayerBaseState OnPlayerDying(Player player, Node area, Vector2 position, Constants.EntityType entityType)
    {
        if (entityType != Constants.EntityType.FALLZONE)
        {
            player.velocity = Vector2.Zero;
        }

        var dyingState = (PlayerDyingState)player.states_store.GetState(PlayerStatesEnum.DYING);
        SetPlayerDeathAnimationType(dyingState, entityType);
        return dyingState;
    }

    public PlayerBaseState OnLand(Player player)
    {
        player.current_animation = player.scale_animation;
        if (!player.current_animation.IsRunning())
        {
            player.current_animation.Start();
        }
        return null;
    }

    protected BaseState<Player> OnDash(Player player)
    {
        var dashingState = (PlayerDashingState)player.states_store.GetState(PlayerStatesEnum.DASHING);
        return dashingState;
    }

    protected BaseState<Player> OnJump(Player player)
    {
        var jumpState = player.states_store.GetState(PlayerStatesEnum.JUMPING);
        return jumpState;
    }

    protected bool JumpPressed(Player player)
    {
        if (player.handle_input_is_disabled) return false;
        return Input.IsActionJustPressed("jump");
    }

    protected BaseState<Player> HandleRotate(Player player)
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
        }
        return null;
    }
}
