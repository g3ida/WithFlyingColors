using Godot;
using Wfc.Entities.World.Player.Explosion;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class PlayerDyingState : PlayerBaseState {
  public DeathAnimationType deathAnimationType = DeathAnimationType.DYING_EXPLOSION;
  private int lightMask;
  private bool fallTimerTriggered = false;

  public PlayerDyingState() : base() {
    this.baseState = PlayerStatesEnum.DYING;
  }

  protected override void _Enter(Player player) {
    fallTimerTriggered = false;
    lightMask = player.lightOccluder.LightMask;
    player.HideColorAreas();
    player.SetCollisionShapesDisabledFlagDeferred(true);
    if (deathAnimationType == DeathAnimationType.DYING_EXPLOSION_REAL) {
      CallDeferred(nameof(CreateExplosion), player);
      EventHandler.Instance.EmitPlayerExplode();
      player.lightOccluder.LightMask = 0;
      player.animatedSpriteNode.Play("die");
    }
    else if (deathAnimationType == DeathAnimationType.DYING_EXPLOSION) {
      EventHandler.Instance.EmitPlayerExplode();
      player.lightOccluder.LightMask = 0;
      player.animatedSpriteNode.Play("die");
      player.animatedSpriteNode.Connect("animation_finished", new Callable(this, nameof(OnAnimationFinished)));
    }
    else // this is the falling case
    {
      EventHandler.Instance.EmitPlayerFall();
      player.fallTimerNode.Start();
      player.fallTimerNode.Timeout += OnFallTimeout;
      fallTimerTriggered = true;
      //player.fallTimerNode.Connect("timeout", new Callable(this, nameof(OnFallTimeout)), (uint)ConnectFlags.OneShot);
    }
  }

  private void OnAnimationFinished(Player player) {
    player.animatedSpriteNode.Disconnect("animation_finished", new Callable(this, nameof(OnAnimationFinished)));
    player.lightOccluder.LightMask = lightMask;
    EventHandler.Instance.EmitPlayerDied();
  }

  private void OnFallTimeout() {
    EventHandler.Instance.EmitPlayerDied();
  }

  protected override BaseState<Player> _PhysicsUpdate(Player player, float delta) {
    return base._PhysicsUpdate(player, delta);
  }

  public BaseState<Player> OnPlayerDying(Node area, Vector2 position, int entityType) {
    return null;
  }

  protected override void _Exit(Player player) {
    if (fallTimerTriggered) {
      fallTimerTriggered = false;
      player.fallTimerNode.Timeout -= OnFallTimeout;
    }
    player.ShowColorAreas();
    player.SetCollisionShapesDisabledFlagDeferred(false);
  }

  private void CreateExplosion(Player player) {
    var explosion = SceneHelpers.InstantiateNode<Explosion>();
    explosion.Connect(nameof(Explosion.ObjectDetonated), new Callable(this, nameof(OnObjectDetonated)), flags: (uint)ConnectFlags.OneShot);
    explosion.Connect("ready", Callable.From(() => {
      explosion.Setup();
      explosion.FireExplosion();
    }), (uint)ConnectFlags.OneShot);
    player.AddChild(explosion);
    explosion.Owner = player;
  }

  private void OnObjectDetonated(Node explosion) {
    explosion.QueueFree();
    EventHandler.Instance.EmitPlayerDied();
  }
}
