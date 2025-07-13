namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Core.Input;
using Wfc.Entities.World.Explosion;
using Wfc.State;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class PlayerDyingState : PlayerBaseState {
  public DeathAnimationType deathAnimationType = DeathAnimationType.Explosion;
  private int lightMask;
  private bool fallTimerTriggered = false;

  public PlayerDyingState(IPlayerStatesStore statesStore, IInputManager inputManager)
    : base(statesStore, inputManager) {
  }

  protected override void _Enter(Player player) {
    fallTimerTriggered = false;
    lightMask = player.LightOccluder.LightMask;
    player.HideColorAreas();
    player.SetCollisionShapesDisabledFlagDeferred(true);
    if (deathAnimationType == DeathAnimationType.ExplosionReal) {
      CallDeferred(nameof(CreateExplosion), player);
      EventHandler.Instance.EmitPlayerExplode();
      player.LightOccluder.LightMask = 0;
      player.AnimatedSpriteNode.Play("die");
    }
    else if (deathAnimationType == DeathAnimationType.Explosion) {
      EventHandler.Instance.EmitPlayerExplode();
      player.LightOccluder.LightMask = 0;
      player.AnimatedSpriteNode.Play("die");
      player.AnimatedSpriteNode.Connect(
        AnimatedSprite2D.SignalName.AnimationFinished,
        new Callable(this, nameof(OnAnimationFinished))
      );
    }
    else // this is the falling case
    {
      EventHandler.Instance.EmitPlayerFall();
      player.FallTimerNode.Start();
      player.FallTimerNode.Timeout += OnFallTimeout;
      fallTimerTriggered = true;
      //player.fallTimerNode.Connect(Timer.SignalName.Timeout, new Callable(this, nameof(OnFallTimeout)), (uint)ConnectFlags.OneShot);
    }
  }

  private void OnAnimationFinished(Player player) {
    player.AnimatedSpriteNode.Disconnect(
      AnimatedSprite2D.SignalName.AnimationFinished,
      new Callable(this, nameof(OnAnimationFinished))
    );
    player.LightOccluder.LightMask = lightMask;
    EventHandler.Instance.EmitPlayerDied();
  }

  private void OnFallTimeout() {
    EventHandler.Instance.EmitPlayerDied();
  }

  protected override IState<Player>? _PhysicsUpdate(Player player, float delta) {
    return base._PhysicsUpdate(player, delta);
  }

  public IState<Player>? OnPlayerDying(Node area, Vector2 position, int entityType) {
    return null;
  }

  protected override void _Exit(Player player) {
    if (fallTimerTriggered) {
      fallTimerTriggered = false;
      player.FallTimerNode.Timeout -= OnFallTimeout;
    }
    player.ShowColorAreas();
    player.SetCollisionShapesDisabledFlagDeferred(false);
  }

  private void CreateExplosion(Player player) {
    var explosion = SceneHelpers.InstantiateNode<Explosion>();
    explosion.Connect(nameof(Explosion.ObjectDetonated), new Callable(this, nameof(OnObjectDetonated)), flags: (uint)ConnectFlags.OneShot);
    explosion.Connect(Node.SignalName.Ready, Callable.From(() => {
      explosion.Setup();
      explosion.FireExplosion();
    }), (uint)ConnectFlags.OneShot);
    player.AddChild(explosion);
    explosion.Owner = player;
  }

  private static void OnObjectDetonated(Node explosion) {
    explosion.QueueFree();
    EventHandler.Instance.EmitPlayerDied();
  }
}
