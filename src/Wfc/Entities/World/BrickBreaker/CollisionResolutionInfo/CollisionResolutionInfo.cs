namespace Wfc.Entities.World.BrickBreaker;

using System;
using Godot;

sealed class CollisionResolutionInfo {
  private const float SIDE_COLLISION_NORMAL_THRESHOLD = 0.8f;

  public KinematicCollision2D Collision;
  public float Angle;
  public float AngleDeg;
  public CharacterBody2D Player;
  public bool IsPlayer;
  public bool IsWall = false;
  public bool IsSideCol;
  public float SideRatio;
  public float Side;
  public Vector2 N;
  public Vector2 U;
  public Vector2 W;

  public CollisionResolutionInfo(KinematicCollision2D _collision, BouncingBall ball) {
    Collision = _collision;
    Angle = ball.BallVelocity.Angle();
    AngleDeg = Mathf.RadToDeg(Angle);
    Player = Global.Instance().Player;
    IsPlayer = Global.Instance().Player == Collision.GetCollider();
    if (Collision.GetCollider() is Node2D colliderNode) {
      IsWall = colliderNode.IsInGroup("wall");
    }
    IsSideCol = IsSideCollision(_collision);
    SideRatio = !IsSideCol ? -1 : ball.RelativeCollisionRatioToSide();
    N = Collision.GetNormal();
    U = ball.BallVelocity.Dot(N) * N;
    W = ball.BallVelocity - U;
    Side = Math.Sign(Player.GlobalPosition.X - Collision.GetPosition().X);
  }

  private static bool IsSideCollision(KinematicCollision2D collision) {
    if (collision.GetCollider() != Global.Instance().Player) {
      return false;
    }
    return Math.Abs(collision.GetNormal().Y) < SIDE_COLLISION_NORMAL_THRESHOLD;
  }
}