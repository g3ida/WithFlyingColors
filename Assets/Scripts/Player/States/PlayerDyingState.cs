using Godot;
using System;

public partial class PlayerDyingState : PlayerBaseState
{
    private static readonly PackedScene ExplosionScene = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/Explosion/Explosion.tscn");

    public DeathAnimationType deathAnimationType = DeathAnimationType.DYING_EXPLOSION;
    private int lightMask;
    private bool fallTimerTriggered = false;

    public PlayerDyingState() : base()
    {
        this.baseState = PlayerStatesEnum.DYING;
    }

    protected override void _Enter(Player player)
    {
        fallTimerTriggered = false;
        lightMask = player.lightOccluder.LightMask;
        player.HideColorAreas();
        player.SetCollisionShapesDisabledFlagDeferred(true);
        if (deathAnimationType == DeathAnimationType.DYING_EXPLOSION_REAL)
        {
            CallDeferred(nameof(CreateExplosion), player);
            Event.Instance().EmitPlayerExplode();
            player.lightOccluder.LightMask = 0;
            player.animatedSpriteNode.Play("die");
        }
        else if (deathAnimationType == DeathAnimationType.DYING_EXPLOSION)
        {
            Event.Instance().EmitPlayerExplode();
            player.lightOccluder.LightMask = 0;
            player.animatedSpriteNode.Play("die");
            player.animatedSpriteNode.Connect("animation_finished", new Callable(this, nameof(OnAnimationFinished)));
        }
        else // this is the falling case
        {
            Event.Instance().EmitPlayerFall();
            player.fallTimerNode.Start();
            player.fallTimerNode.Timeout += OnFallTimeout;
            fallTimerTriggered = true;
            //player.fallTimerNode.Connect("timeout", new Callable(this, nameof(OnFallTimeout)), (uint)ConnectFlags.OneShot);
        }
    }

    private void OnAnimationFinished(Player player)
    {
        player.animatedSpriteNode.Disconnect("animation_finished", new Callable(this, nameof(OnAnimationFinished)));
        player.lightOccluder.LightMask = lightMask;
        Event.Instance().EmitPlayerDied();
    }

    private void OnFallTimeout()
    {
        Event.Instance().EmitPlayerDied();
    }

    protected override BaseState<Player> _PhysicsUpdate(Player player, float delta)
    {
        return base._PhysicsUpdate(player, delta);
    }

    public BaseState<Player> OnPlayerDying(Node area, Vector2 position, int entityType)
    {
        return null;
    }

    protected override void _Exit(Player player)
    {
        if (fallTimerTriggered) {
            fallTimerTriggered = false;
            player.fallTimerNode.Timeout -= OnFallTimeout;
        }
        player.ShowColorAreas();
        player.SetCollisionShapesDisabledFlagDeferred(false);
    }

    private void CreateExplosion(Player player)
    {
        var explosion = ExplosionScene.Instantiate<Explosion>();
        explosion.player = player;
        explosion.playerTexture = Global.Instance().GetPlayerSprite();
        explosion.Connect(nameof(Explosion.ObjectDetonated), new Callable(this, nameof(OnObjectDetonated)), flags: (uint)ConnectFlags.OneShot);
        explosion.Connect("ready", Callable.From(() => {
            explosion.Setup();
            explosion.FireExplosion();
        }), (uint)ConnectFlags.OneShot);
        player.AddChild(explosion);
        explosion.Owner = player;
    }

    private void OnObjectDetonated(Node explosion)
    {
        explosion.QueueFree();
        Event.Instance().EmitPlayerDied();
    }
}
