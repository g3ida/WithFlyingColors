using Godot;
using System;

public class LevelUp : Node2D
{
    private AnimationPlayer animationNode;
    private AnimationPlayer labelAnimationNode;
    private int animationCount = 0;

    public override void _Ready()
    {
        animationNode = GetNode<AnimationPlayer>("AnimationPlayer");
        labelAnimationNode = GetNode<AnimationPlayer>("Label/LabelAnimation");

        animationNode.Play("Scale");
        labelAnimationNode.Play("Fade");
    }

    private void _on_LabelAnimation_animation_finished(string animName)
    {
        OnAnimEnd();
    }

    private void _on_AnimationPlayer_animation_finished(string animName)
    {
        OnAnimEnd();
    }

    private void OnAnimEnd()
    {
        animationCount++;
        if (animationCount == 2)
        {
            QueueFree();
        }
    }
}
