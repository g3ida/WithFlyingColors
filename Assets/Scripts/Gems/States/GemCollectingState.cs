using Godot;
using System;

public class GemCollectingState : GemBaseState
{
    private bool isActive = false;

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

    public  override BaseState<Gem> OnAnimationFinished(Gem gem, string animName)
    {
        if (animName == "gem_collected_animation")
        {
            Event.Instance().EmitSignal(
                "gem_collected",
                gem.group_name,
                gem.AnimatedSpriteNode.GetGlobalTransformWithCanvas().origin,
                gem.AnimatedSpriteNode.Frames);
            return gem.StatesStore.Collected;
        }
        return null;
    }
}
