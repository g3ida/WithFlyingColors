namespace Wfc.Entities.World.Gems;

using Godot;
using Wfc.State;
using static Godot.AnimationMixer;
using EventHandler = Wfc.Core.Event.EventHandler;

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

  public override IState<Gem>? PhysicsUpdate(Gem gem, float delta) => _requestedState;

  private GemCollectedState? _handleAnimationFinished(Gem gem) {
    // A ghost is taken the same way it is dropped into the world: it plays out and
    // goes. Announcing it would fly a gem into a HUD slot that has been full since
    // the level opened.
    if (!gem.IsAlreadyCollected) {
      EventHandler.Instance.EmitGemCollected(
          gem.GroupName,
          gem.AnimatedSpriteNode.GetGlobalTransformWithCanvas().Origin,
          gem.AnimatedSpriteNode.SpriteFrames);
    }
    return _statesStore.GetState<GemCollectedState>();
  }
}
