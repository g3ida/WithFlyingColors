using Godot;
using System;
using System.Collections.Generic;

public class TripleBallsPowerUp : PowerUpScript
{
    private bool isDone = false;

    public override void _Ready()
    {
        SetProcess(false);
        List<BouncingBall> bouncingBalls = new List<BouncingBall>();
        foreach (Node c in BrickBreakerNode.BallsContainer.GetChildren())
        {
            if (c is BouncingBall bouncingBall)
            {
                bouncingBalls.Add(bouncingBall);
            }
        }

        foreach (BouncingBall b in bouncingBalls)
        {
            for (int i = 0; i < 2; i++)
            {
                BouncingBall ball = BrickBreakerNode.SpawnBall(b.color_group) as BouncingBall;
                ball.Position = b.Position;
                Vector2 spawnVelocity = b.velocity.Rotated((i - 0.5f) * 2 * Constants.PI3);
                if (spawnVelocity.y > 0)
                {
                    spawnVelocity.y = -spawnVelocity.y;
                }
                ball.CallDeferred("SetVelocity", spawnVelocity);
            }
        }

        isDone = true;
    }

    public override bool IsStillRelevant()
    {
        return !isDone;
    }
}
