namespace Wfc.Entities.World.Gems;

using Godot;
using Wfc.State;
using static Godot.AnimationMixer;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class GemCollectingState : GemBaseState {

  private AnimationFinishedEventHandler? _animationFinishedEventHandler;
  private BaseState<Gem>? _requestedState = null;
  private StringName _gemCollectedAnimationStringName = "gem_collected_animation";
  private uint _cachedCollisionMask = 0;

  public GemCollectingState() : base() {
  }

  public override void Enter(Gem gem) {
    gem.CollisionShapeNode.Disabled = true;
    _cachedCollisionMask = gem.CollisionMask;
    gem.CollisionMask = 0;
    gem.AnimationPlayerNode.Play(_gemCollectedAnimationStringName);
    gem.ShineSfxNode.Stop();

    _animationFinishedEventHandler = (StringName animName) => {
      if (animName == _gemCollectedAnimationStringName) {
        _requestedState = _handleAnimationFinished(gem);
      }
    };
    gem.AnimationPlayerNode.AnimationFinished += _animationFinishedEventHandler;
  }

  public override void Exit(Gem gem) {
    gem.CollisionShapeNode.Disabled = false;
    gem.CollisionMask = _cachedCollisionMask;
    _cachedCollisionMask = 0;
    if (_animationFinishedEventHandler != null) {
      gem.AnimationPlayerNode.AnimationFinished -= _animationFinishedEventHandler;
      _animationFinishedEventHandler = null;
    }
  }

  public override BaseState<Gem>? PhysicsUpdate(Gem gem, float delta) => _requestedState;

  private static BaseState<Gem>? _handleAnimationFinished(Gem gem) {
    EventHandler.Instance.EmitGemCollected(
        gem.group_name,
        gem.AnimatedSpriteNode.GetGlobalTransformWithCanvas().Origin,
        gem.AnimatedSpriteNode.SpriteFrames);
    return gem.StatesStore.Collected;
  }
}
