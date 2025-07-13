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
  private IStatesStore<Gem> _statesStore;

  public GemCollectingState(IStatesStore<Gem> statesStore) : base() {
    _statesStore = statesStore;
  }

  public override void Enter(Gem o) {
    o.CollisionShapeNode.Disabled = true;
    _cachedCollisionMask = o.CollisionMask;
    o.CollisionMask = 0;
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
    _cachedCollisionMask = 0;
    if (_animationFinishedEventHandler != null) {
      o.AnimationPlayerNode.AnimationFinished -= _animationFinishedEventHandler;
      _animationFinishedEventHandler = null;
    }
  }

  public override IState<Gem>? PhysicsUpdate(Gem gem, float delta) => _requestedState;

  private GemCollectedState? _handleAnimationFinished(Gem gem) {
    EventHandler.Instance.EmitGemCollected(
        gem.group_name,
        gem.AnimatedSpriteNode.GetGlobalTransformWithCanvas().Origin,
        gem.AnimatedSpriteNode.SpriteFrames);
    return _statesStore.GetState<GemCollectedState>();
  }
}
