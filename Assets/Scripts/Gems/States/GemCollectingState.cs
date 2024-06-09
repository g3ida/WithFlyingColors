using Godot;
using System;

public partial class GemCollectingState : GemBaseState
{
  public GemCollectingState() : base()
  {
  }

  public override void Enter(Gem gem)
  {
    gem.CollisionShapeNode.SetDeferred("disabled", true);
    gem.AnimationPlayerNode.Play("gem_collected_animation");
    gem.ShineNode.Stop();
  }

  public override void Exit(Gem gem)
  {
    gem.CollisionShapeNode.SetDeferred("disabled", false);
  }

  public override BaseState<Gem> OnAnimationFinished(Gem gem, string animName)
  {
    if (animName == "gem_collected_animation")
    {
      Event.Instance.EmitGemCollected(
          gem.group_name,
          gem.AnimatedSpriteNode.GetGlobalTransformWithCanvas().Origin,
          gem.AnimatedSpriteNode.SpriteFrames);
      return gem.StatesStore.Collected;
    }
    return null;
  }
}
