namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.Entities.World.Explosion;
using Wfc.State;
using Wfc.Utils;

public partial class PlayerExplosionState : PlayerDyingBaseState {
  private int lightMask;
  public PlayerExplosionState(IPlayerStatesStore statesStore, IInputManager inputManager)
    : base(statesStore, inputManager) {
  }

  protected override void _Enter(Player player) {
    base._Enter(player);
    // A cube that blows up stops where it was. The fall zone is the other way to die and the only
    // one that keeps its momentum, because the fall is the whole of what the player sees.
    player.Velocity = Vector2.Zero;
    lightMask = player.LightOccluder.OccluderLightMask;
    // create the explosion
    Callable.From(() => CreateExplosion(player)).CallDeferred();
    GameEvents.Instance.OnPlayerExploded();
    player.LightOccluder.OccluderLightMask = 0;
    player.AnimatedSpriteNode.Play("die");
  }

  protected override void _Exit(Player player) {
    base._Exit(player);
    player.LightOccluder.OccluderLightMask = lightMask;
  }

  private static void CreateExplosion(Player player) {
    var explosion = SceneHelpers.InstantiateNode<Explosion>();
    explosion.Connect(
      nameof(Explosion.ObjectDetonated),
      Callable.From<Node>(OnObjectDetonated),
      flags: (uint)GodotObject.ConnectFlags.OneShot);
    explosion.Connect(Node.SignalName.Ready, Callable.From(() => {
      explosion.Setup(player);
      explosion.FireExplosion();
    }), (uint)GodotObject.ConnectFlags.OneShot);
    player.AddChild(explosion);
    explosion.Owner = player;
  }

  private static void OnObjectDetonated(Node explosion) {
    explosion.QueueFree();
    GameEvents.Instance.OnPlayerDied();
  }
}
