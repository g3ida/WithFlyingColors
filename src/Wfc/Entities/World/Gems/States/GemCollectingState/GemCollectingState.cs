namespace Wfc.Entities.World.Gems;

using Godot;
using Wfc.Core.Event;
using Wfc.State;
using Wfc.Utils;
using static Godot.AnimationMixer;

public partial class GemCollectingState : GemBaseState {

  private AnimationFinishedEventHandler? _animationFinishedEventHandler;
  private IState<Gem>? _requestedState = null;
  private StringName _gemCollectedAnimationStringName = "gem_collected_animation";
  private uint _cachedCollisionMask = 0;
  private uint _cachedCollisionLayer = 0;

  private IStatesStore<Gem> _statesStore;

  public GemCollectingState(IStatesStore<Gem> statesStore) : base() {
    _statesStore = statesStore;
  }

  public override void Enter(Gem o) {
    // Shared instance: see GemNotCollectedState.Enter. A stale request here skips straight
    // to GemCollectedState without ever waiting for the animation.
    _requestedState = null;

    o.CollisionShapeNode.Disabled = true;
    _cachedCollisionMask = o.CollisionMask;
    _cachedCollisionLayer = o.CollisionLayer;
    o.CollisionMask = 0;
    o.CollisionLayer = 0;
    o.AnimationPlayerNode.Play(_gemCollectedAnimationStringName);
    o.ShineSfxNode.Stop();
    _spawnBurst(o);

    _animationFinishedEventHandler = (StringName animName) => {
      if (animName == _gemCollectedAnimationStringName) {
        _requestedState = _handleAnimationFinished(o);
      }
    };
    o.AnimationPlayerNode.AnimationFinished += _animationFinishedEventHandler;
  }

  public override void Exit(Gem o) {
    o.CollisionShapeNode.Disabled = false;
    o.CollisionMask = _cachedCollisionMask;
    o.CollisionLayer = _cachedCollisionLayer;
    _cachedCollisionMask = 0;
    _cachedCollisionLayer = 0;
    if (_animationFinishedEventHandler != null) {
      o.AnimationPlayerNode.AnimationFinished -= _animationFinishedEventHandler;
      _animationFinishedEventHandler = null;
    }
  }

  public override IState<Gem>? PhysicsUpdate(Gem gem, float delta) {
    // The shine rides up with the gem instead of staying behind at the pickup point.
    gem.LightNode.Position = gem.AnimatedSpriteNode.Position;
    return _requestedState;
  }

  // The burst is left standing in the level beside the gem, since anything hanging off the
  // gem itself would be hidden along with it the moment the animation ends. A gem the level
  // has already given up flares as faintly as it shines.
  private static void _spawnBurst(Gem gem) {
    if (gem.GetParent() is not Node level) {
      return;
    }
    var burst = SceneHelpers.InstantiateNode<GemCollectBurst>();
    burst.Setup(gem.CoreColor, gem.LightEnergyScale);
    burst.Position = gem.Position;
    level.AddChild(burst);
  }

  private GemCollectedState? _handleAnimationFinished(Gem gem) {
    // A ghost is taken the same way it is dropped into the world: it plays out and
    // goes. Announcing it would fly a gem into a HUD slot that has been full since
    // the level opened.
    if (!gem.IsAlreadyCollected) {
      GameEvents.Instance.OnGemCollected(
          gem.GroupName,
          gem.AnimatedSpriteNode.GetGlobalTransformWithCanvas().Origin,
          gem.AnimatedSpriteNode.SpriteFrames);
    }
    return _statesStore.GetState<GemCollectedState>();
  }
}
