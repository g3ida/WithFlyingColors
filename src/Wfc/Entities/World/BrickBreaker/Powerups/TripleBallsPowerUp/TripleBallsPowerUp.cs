namespace Wfc.Entities.World.BrickBreaker.Powerups;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Utils;

public partial class TripleBallsPowerUp : PowerUpScript {
  private bool isDone = false;

  public override void _Ready() {
    base._Ready();
    SetProcess(false);
    if (BrickBreakerNode != null) {
      List<BouncingBall> bouncingBalls = new List<BouncingBall>();
      foreach (Node c in BrickBreakerNode.BallsContainer.GetChildren()) {
        if (c is BouncingBall bouncingBall) {
          bouncingBalls.Add(bouncingBall);
        }
      }

      foreach (BouncingBall b in bouncingBalls) {
        for (int i = 0; i < 2; i++) {
          BouncingBall ball = BrickBreakerNode.SpawnBall(b.color_group);
          ball.Position = b.Position;
          Vector2 spawnVelocity = b.BallVelocity.Rotated((i - 0.5f) * 2 * MathUtils.PI3);
          if (spawnVelocity.Y > 0) {
            spawnVelocity.Y = -spawnVelocity.Y;
          }
          ball.BallVelocity = spawnVelocity;
          ball.CallDeferred(BouncingBall.MethodName.SetVelocity, spawnVelocity);
        }
      }
    }

    isDone = true;
  }

  public override bool IsStillRelevant() {
    return !isDone;
  }
}
