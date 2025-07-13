namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Core.Input;
using Wfc.Entities.World.Explosion;
using Wfc.State;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class PlayerExplosionState : PlayerBaseState {
  private int lightMask;
  public PlayerExplosionState(IPlayerStatesStore statesStore, IInputManager inputManager)
    : base(statesStore, inputManager) {
  }

  protected override void _Enter(Player player) {
    lightMask = player.LightOccluder.LightMask;
    player.HideColorAreas();
    player.SetCollisionShapesDisabledFlagDeferred(true);
    // create the explosion
    CallDeferred(nameof(CreateExplosion), player);
    EventHandler.Instance.EmitPlayerExplode();
    player.LightOccluder.LightMask = 0;
    player.AnimatedSpriteNode.Play("die");
  }

  private void OnAnimationFinished(Player player) {
    player.AnimatedSpriteNode.Disconnect(
      AnimatedSprite2D.SignalName.AnimationFinished,
      new Callable(this, nameof(OnAnimationFinished))
    );
    player.LightOccluder.LightMask = lightMask;
    EventHandler.Instance.EmitPlayerDied();
  }

  protected override void _Exit(Player player) {
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
