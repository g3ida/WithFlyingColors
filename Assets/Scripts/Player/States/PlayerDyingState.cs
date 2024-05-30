using Godot;
using System;

public class PlayerDyingState : PlayerBaseState
{
    private static readonly PackedScene ExplosionScene = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/Explosion/Explosion.tscn");

    public DeathAnimationType deathAnimationType = DeathAnimationType.DYING_EXPLOSION;
    private int lightMask;

    public PlayerDyingState() : base()
    {
        this.baseState = PlayerStatesEnum.DYING;
    }

    protected override void _Enter(Player player)
    {
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
            player.animatedSpriteNode.Connect("animation_finished", this, nameof(OnAnimationFinished));
        }
        else // this is the falling case
        {
            Event.Instance().EmitPlayerFall();
            player.fallTimerNode.Start();
            player.fallTimerNode.Connect("timeout", this, nameof(OnFallTimeout), null, (uint)ConnectFlags.Oneshot);
        }
    }

    private void OnAnimationFinished(Player player)
    {
        player.animatedSpriteNode.Disconnect("animation_finished", this, nameof(OnAnimationFinished));
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
        player.ShowColorAreas();
        player.SetCollisionShapesDisabledFlagDeferred(false);
    }

    private void CreateExplosion(Player player)
    {
        var explosion = (Node)ExplosionScene.Instance();
        explosion.Set("player", player);
        explosion.Set("playerTexture", Global.Instance().GetPlayerSprite());
        explosion.Connect("ObjectDetonated", this, nameof(OnObjectDetonated), flags: (uint)ConnectFlags.Oneshot);
        explosion.Connect("ready", this, nameof(OnExplosionReady), new Godot.Collections.Array { explosion }, (uint)ConnectFlags.Oneshot);
        player.AddChild(explosion);
        explosion.Owner = player;
    }

    private void OnExplosionReady(Node explosion)
    {
        explosion.Call("Setup");
        explosion.Call("FireExplosion");
    }

    private void OnObjectDetonated(Node explosion)
    {
        explosion.QueueFree();
        Event.Instance().EmitPlayerDied();
    }
}
