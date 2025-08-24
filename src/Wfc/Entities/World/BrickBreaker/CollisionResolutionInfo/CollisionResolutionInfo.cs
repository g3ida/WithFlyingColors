namespace Wfc.Entities.World.BrickBreaker;

using System;
using Godot;

sealed class CollisionResolutionInfo {
  private const float SIDE_COLLISION_NORMAL_THRESHOLD = 0.8f;

  public KinematicCollision2D Collision { get; private set; }
  public float Angle { get; private set; }
  public float AngleDeg { get; private set; }
  public Player.Player? Player { get; private set; }
  public bool IsPlayer { get; private set; }
  public bool IsWall { get; private set; } = false;
  public bool IsSideCol { get; private set; }
  public float SideRatio { get; private set; }
  public float Side { get; private set; }
  public Vector2 N { get; private set; }
  public Vector2 U { get; private set; }
  public Vector2 W { get; private set; }

  public CollisionResolutionInfo(KinematicCollision2D _collision, BouncingBall ball) {
    Collision = _collision;
    Angle = ball.BallVelocity.Angle();
    AngleDeg = Mathf.RadToDeg(Angle);
    IsPlayer = Collision.GetCollider() is Player.Player;
    Player = Collision.GetCollider() as Player.Player;
    if (Collision.GetCollider() is Node2D colliderNode) {
      IsWall = colliderNode.IsInGroup("wall");
    }
    IsSideCol = IsSideCollision();
    if (IsSideCol && Player is Player.Player player) {
      SideRatio = ball.RelativeCollisionRatioToSide(player);
      Side = Math.Sign(Player.GlobalPosition.X - Collision.GetPosition().X);
    }
    else {
      SideRatio = -1;
      Side = 0;
    }
    N = Collision.GetNormal();
    U = ball.BallVelocity.Dot(N) * N;
    W = ball.BallVelocity - U;
  }

  private bool IsSideCollision() {
    if (IsPlayer) {
      return Math.Abs(Collision.GetNormal().Y) < SIDE_COLLISION_NORMAL_THRESHOLD;
    }
    return false;
  }
}
