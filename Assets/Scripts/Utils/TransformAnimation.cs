using Godot;
using System;

public class TransformAnimation : Node
{
    private float animationDuration;
    private Interpolation interpolation;
    private float offsetY;

    private float timer = 0.0f;
    private bool started = false;

    public TransformAnimation() {
        // FIXME: This constructor should be removed after c# migration
    }

    public TransformAnimation(float _animationDuration, Interpolation _interpolation, float _offsetY)
    {
        animationDuration = _animationDuration;
        interpolation = _interpolation;
        offsetY = _offsetY;
    }

    public bool IsDone()
    {
        return timer <= 0.0f && started;
    }

    public bool IsRunning()
    {
        return timer > 0.0f;
    }

    public void Start()
    {
        if (IsRunning())
            return;

        timer = animationDuration;
        started = true;
    }

    public void Step(Node2D node, AnimatedSprite sprite, float delta)
    {
        if (timer > 0)
        {
            float sinRot = Mathf.Sin(node.Rotation);
            float cosRot = Mathf.Cos(node.Rotation);
            float normalized = timer / animationDuration;
            float mean = 1.0f;
            float i = interpolation.Apply(0.0f, 1.0f, normalized) - mean;

            sprite.Scale = new Vector2(
                mean + (i * Mathf.Abs(cosRot) - Mathf.Abs(sinRot) * i),
                mean + (i * Mathf.Abs(sinRot) - Mathf.Abs(cosRot) * i)
            );

            timer -= delta;
            sprite.Offset = new Vector2(
                offsetY * (1.0f - sprite.Scale.x) * sinRot,
                offsetY * (1.0f - sprite.Scale.y) * cosRot
            );
        }
        else
        {
            Reset(sprite);
        }
    }

    public void Reset(AnimatedSprite sprite)
    {
        timer = 0.0f;
        started = false;
        sprite.Scale = new Vector2(1.0f, 1.0f);
        sprite.Offset = new Vector2(0.0f, 0.0f);
    }
}
