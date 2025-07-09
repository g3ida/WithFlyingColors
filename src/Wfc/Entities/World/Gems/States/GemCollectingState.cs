namespace Wfc.Entities.World.Gems;

using Godot;
using Wfc.State;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class GemCollectingState : GemBaseState {
  public GemCollectingState() : base() {
  }

  public override void Enter(Gem gem) {
    gem.CollisionShapeNode.SetDeferred(CollisionPolygon2D.PropertyName.Disabled, true);
    gem.AnimationPlayerNode.Play("gem_collected_animation");
    gem.ShineSfxNode.Stop();
  }

  public override void Exit(Gem gem) {
    gem.CollisionShapeNode.SetDeferred(CollisionPolygon2D.PropertyName.Disabled, false);
  }

  public override BaseState<Gem>? OnAnimationFinished(Gem gem, string animName) {
    if (animName == "gem_collected_animation") {
      EventHandler.Instance.EmitGemCollected(
          gem.group_name,
          gem.AnimatedSpriteNode.GetGlobalTransformWithCanvas().Origin,
          gem.AnimatedSpriteNode.SpriteFrames);
      return gem.StatesStore.Collected;
    }
    return null;
  }
}
