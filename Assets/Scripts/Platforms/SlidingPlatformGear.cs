using Godot;
using System;

public class SlidingPlatformGear : Sprite
{
    private Vector2 lastPosition;
    private const float RotationSpeed = 0.01f;

    public override void _Ready()
    {
        // Set parent scale so the sprite won't be affected.
        var parentPlatformScale = GetParent().GetParent<Node2D>().Scale;
        GetParent<Node2D>().Scale = new Vector2(1 / parentPlatformScale.x, 1 / parentPlatformScale.y);
        lastPosition = GlobalPosition;
    }

    public override void _PhysicsProcess(float delta)
    {
        var currentPosition = GlobalPosition;
        var deltaPosition = currentPosition - lastPosition;
        lastPosition = currentPosition;

        int direction = 0;
        if (deltaPosition.x > Constants.EPSILON2 || deltaPosition.y > Constants.EPSILON2)
        {
            direction = 1;
        }
        else if (deltaPosition.x < -Constants.EPSILON2 || deltaPosition.y < -Constants.EPSILON2)
        {
            direction = -1;
        }

        Rotate(RotationSpeed * deltaPosition.Length() * direction);
    }
}
