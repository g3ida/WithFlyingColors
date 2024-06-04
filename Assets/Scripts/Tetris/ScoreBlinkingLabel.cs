using Godot;

public partial class ScoreBlinkingLabel : Label
{
    private AnimationPlayer animationPlayerNode;

    public override void _Ready()
    {
        animationPlayerNode = GetNode<AnimationPlayer>("AnimationPlayer");
    }

    public void SetValue(string value)
    {
        Text = value;
        animationPlayerNode.Play("Blink");
    }
}
