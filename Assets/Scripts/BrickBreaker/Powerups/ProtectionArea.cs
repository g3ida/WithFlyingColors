using Godot;

public class ProtectionArea : StaticBody2D
{
    public override void _Ready()
    {
        // Initialization code goes here
    }

    public void _on_Area2D_body_entered(Node body)
    {
        if (body is BouncingBall)
        {
            QueueFree();
        }
    }
}
