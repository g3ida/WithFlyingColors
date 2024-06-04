using Godot;
using System;

public partial class PlayerDashingState : PlayerBaseState
{
    private static readonly PackedScene DashGhost = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/Player/DashGhost.tscn");
    private const float DASH_DURATION = 0.17f;
    private const float PERMISSIVENESS = 0.05f;
    private const float DASH_SPEED = 20 * Constants.WORLD_TO_SCREEN;
    private const float DASH_GHOST_INSTANCE_DELAY = 0.04f;

    private CountdownTimer dashTimer = new CountdownTimer();
    private CountdownTimer permissivenessTimer = new CountdownTimer();
    public Vector2 direction = Vector2.Zero;
    private bool dashDone = false;

    public PlayerDashingState() : base() {
        dashTimer.Set(DASH_DURATION, false);
        permissivenessTimer.Set(PERMISSIVENESS, false);
        this.baseState = PlayerStatesEnum.DASHING;
    }

    protected override void _Enter(Player player)
    {
        player.dashGhostTimerNode.WaitTime = DASH_GHOST_INSTANCE_DELAY;
        player.dashGhostTimerNode.Connect("timeout", new Callable(this, "_OnDashGhostTimerTimeout"));
        if (direction == Vector2.Zero)
        {
            permissivenessTimer.Reset();
            SetDashDirection(player);
            dashDone = false;
        }
        else
        {
            dashDone = true;
            permissivenessTimer.Stop();
        }

        dashTimer.Reset();
        player.can_dash = false;
        Global.Instance().Camera.GetNode<CameraShake>("CameraShake").Start();
        InstanceGhost(player);
        player.dashGhostTimerNode.Start();
    }

    protected override void _Exit(Player player)
    {
        if (dashDone)
        {
            player.velocity.X = 0;
        }
        dashTimer.Stop();
        permissivenessTimer.Stop();
        player.dashGhostTimerNode.Stop();
        player.dashGhostTimerNode.Disconnect("timeout", new Callable(this, "_OnDashGhostTimerTimeout"));
        direction = Vector2.Zero;
    }

    protected override BaseState<Player> _PhysicsUpdate(Player player, float delta)
    {
        if (!dashDone && !permissivenessTimer.IsRunning())
        {
            SetDashDirection(player);
            if (direction.LengthSquared() < 0.01f)
            {
                dashTimer.Stop();
            }
            else
            {
                dashDone = true;
                Event.Instance().EmitPlayerDash(direction);
            }
        }

        if (dashDone)
        {
            if (Mathf.Abs(direction.X) > 0.01f)
            {
                player.velocity.X = DASH_SPEED * direction.X;
            }
            if (Mathf.Abs(direction.Y) > 0.01f)
            {
                player.velocity.Y = DASH_SPEED * direction.Y;
            }
        }

        if (!dashTimer.IsRunning())
        {
            return player.states_store.GetState(PlayerStatesEnum.FALLING);
        }
        else
        {
            player.velocity.Y = 0;
        }

        dashTimer.Step(delta);
        permissivenessTimer.Step(delta);

        return null;
    }

    private void SetDashDirection(Player player)
    {
        direction = Vector2.Zero;
        if (Input.IsActionPressed("move_right") && Input.IsActionPressed("move_left"))
        {
            direction.X = 0;
        }
        else if (Input.IsActionPressed("move_left"))
        {
            direction.X = -1;
        }
        else if (Input.IsActionPressed("move_right"))
        {
            direction.X = 1;
        }
        else if (Mathf.Abs(player.velocity.X) > 0.1f)
        {
            direction.X = 1 * Mathf.Sign(player.velocity.X);
        }
        else
        {
            direction.X = 0;
        }
        if (Input.IsActionPressed("down"))
        {
            direction.Y = 1;
        }
    }

    private void _OnDashGhostTimerTimeout()
    {
        InstanceGhost(Global.Instance().Player);
    }

    private void InstanceGhost(Player player)
    {
        Sprite2D ghost = DashGhost.Instantiate<Sprite2D>();
        ghost.Scale = player.Scale;
        player.GetParent().AddChild(ghost);
        ghost.GlobalPosition = player.GlobalPosition;
        ghost.Texture = player.animatedSpriteNode.SpriteFrames.GetFrameTexture(player.animatedSpriteNode.Animation, player.animatedSpriteNode.Frame);
        ghost.Rotate(player.Rotation);
    }
}
