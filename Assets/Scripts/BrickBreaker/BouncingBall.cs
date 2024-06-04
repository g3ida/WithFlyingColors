using Godot;
using System;
using System.Linq;

public class BouncingBall : KinematicBody2D
{
    [Export]
    public string color_group = "blue";

    private Area2D AreaNode;
    private BouncingBallSprite SpriteNode;
    private CollisionShape2D AreaCollisionShape;
    private Timer IntersectionTimer;

    private const float DEVIATION = 10.0f;
    private const float SPEED = 420.0f;
    private static readonly Vector2 SPAWN_DIRECTION = Vector2.Up;
    private const float SPAWN_DIRECTION_RANDOM_DEGREES = 45.0f;
    private const float DEVIATION_THRESHOLD = 5.0f;
    private const float DEVIATION_DEGREES_ADDED = 10.0f;
    private const float SIDE_COLLISION_NORMAL_THRESHOLD = 0.8f;
    private const float PLAYER_SIDE_HIT_PUSH_VELOCITY = 250.0f;
    private const float SPEED_UNIT = 60.0f;

    public Vector2 velocity = Vector2.Up * SPEED;
    private float speed = SPEED;
    public Area2D DeathZone = null; // FIXME: this is set in breakBreaker. better logic ?
    private int player_last_direction;

    private class CollisionResolutionInfo
    {
        public KinematicCollision2D collision;
        public float angle;
        public float angle_deg;
        public KinematicBody2D player;
        public bool is_player;
        public bool is_wall = false;
        public bool is_side_col;
        public float side_ratio;
        public float side;
        public Vector2 n;
        public Vector2 u;
        public Vector2 w;

        public CollisionResolutionInfo(KinematicCollision2D _collision, BouncingBall ball)
        {
            collision = _collision;
            angle = ball.velocity.Angle();
            angle_deg = Mathf.Rad2Deg(angle);
            player = Global.Instance().Player;
            is_player = Global.Instance().Player == collision.Collider;
            if (collision.Collider is Node2D colliderNode) {
              is_wall = colliderNode.IsInGroup("wall");
            }
            is_side_col = ball.IsSideCollision(_collision);
            side_ratio = !is_side_col ? -1 : ball.RelativeCollisionRatioToSide();
            n = collision.Normal;
            u = ball.velocity.Dot(n) * n;
            w = ball.velocity - u;
            side = Math.Sign(player.GlobalPosition.x - collision.Position.x);
        }
    }

    public override void _Ready()
    {
        SpriteNode = GetNode<BouncingBallSprite>("BBSpr");
        AreaNode = GetNode<Area2D>("Area2D");
        AreaCollisionShape = GetNode<CollisionShape2D>("Area2D/CollisionShape2D");
        IntersectionTimer = GetNode<Timer>("IntersectionTimer");
        
        GD.Randomize();
        reset();
        SetColor(color_group);
    }

    public void SetColor(string colorName)
    {
        SpriteNode.SetColor(colorName);
        if (AreaNode.IsInGroup(color_group))
            AreaNode.RemoveFromGroup(color_group);
        AreaNode.AddToGroup(colorName);
        color_group = colorName;
    }

    private bool IsSideCollision(KinematicCollision2D collision)
    {
        if (collision.Collider != Global.Instance().Player)
            return false;
        return Math.Abs(collision.Normal.y) < SIDE_COLLISION_NORMAL_THRESHOLD;
    }

    private float RelativeCollisionRatioToSide()
    {
        var scale_y = Global.Instance().Player.Scale.y;
        var ppos = Global.Instance().GlobalPosition;
        var dims = Global.Instance().Player.GetCollisionShapeSize();
        var ratio = (ppos.y + dims.y * scale_y * 0.5f - GlobalPosition.y) / dims.y * scale_y;
        return Mathf.Clamp(ratio, 0.0f, 1.0f);
    }

    private bool IsFallingStraightAndCollidingWithSide(bool sideCollision, float angleDegrees)
    {
        return sideCollision && velocity.y > Constants.EPSILON && Math.Abs(Math.Abs(angleDegrees) - 90.0f) < 45.0f;
    }

    private bool _HandlePlayerCollision(CollisionResolutionInfo info)
    {
        if (info.is_player)
        {
            Player player = info.player as Player;
            if (!info.is_side_col)
            {
                if (IsBallUnderPlayer())
                    velocity = new Vector2(0, 1);
                else
                {
                    var position = info.collision.Position;
                    var dp = player.GlobalPosition - position;
                    var player_size = (player as Player).GetCollisionShapeSize();
                    var normalized_pos_x = dp.x / player_size.x;
                    var m = new Vector2(info.n.y, info.n.x);
                    velocity = DEVIATION * SPEED_UNIT * normalized_pos_x * m - info.u;
                }
            }
            else // side collision
            {
                if (IsFallingStraightAndCollidingWithSide(info.is_side_col, info.angle_deg))
                    velocity = new Vector2(info.side, 0).Rotated(-info.side * Mathf.Deg2Rad((float)GD.RandRange(0.0f, 5.0f)));
                else
                {
                    velocity = info.w - info.u;
                    if (velocity.y > Constants.EPSILON) //check ratio
                        velocity.y = -velocity.y;
                }
                // avoid player sticking to the ball
                if (player.velocity.x * info.n.x >= 0)
                    player.velocity.x = -info.n.x * PLAYER_SIDE_HIT_PUSH_VELOCITY;
            }
            return true;
        }
        return false;
    }

    private bool _IsVerticalWall(CollisionResolutionInfo info)
    {
        return Mathf.Abs(info.n.x) > 0.5f && Mathf.Abs(info.n.y) < Constants.EPSILON;
    }

    private bool _IsBallAlmostHorizontal(CollisionResolutionInfo info)
    {
        return info.angle_deg < DEVIATION_THRESHOLD || Mathf.Abs(info.angle_deg) > 180.0f - DEVIATION_THRESHOLD;
    }

    private bool _IsBallAlmostVertical()
    {
        return Mathf.Abs(Vector2.Up.AngleTo(velocity)) < DEVIATION_THRESHOLD || Mathf.Abs(Vector2.Down.AngleTo(velocity)) < DEVIATION_THRESHOLD;
    }

    private bool _IsHorizontalWall(CollisionResolutionInfo info)
    {
        return Mathf.Abs(info.n.y) > 0.5f && Mathf.Abs(info.n.x) < Constants.EPSILON;
    }

    private void _HandleDefaultCollision(CollisionResolutionInfo info)
    {
        velocity = info.w - info.u;
        if (info.is_wall)
        {
            if (_IsVerticalWall(info) && _IsBallAlmostHorizontal(info))
                velocity = velocity.Rotated(Math.Sign(velocity.y * info.n.x) * Mathf.Deg2Rad((float)GD.RandRange(0, DEVIATION_DEGREES_ADDED)));
            else if (_IsHorizontalWall(info) && _IsBallAlmostVertical())
                velocity = velocity.Rotated(-Math.Sign(velocity.x) * Mathf.Deg2Rad((float)GD.RandRange(0, DEVIATION_DEGREES_ADDED)));
        }
    }

    private void _SetPlayerLastDirection()
    {
        if (Global.Instance().Player.velocity.x > 0.0f)
            player_last_direction = 1;
        else if (Global.Instance().Player.velocity.x < 0.0f)
            player_last_direction = -1;
    }

public override void _PhysicsProcess(float delta)
    {
        SetPlayerLastDirection();
        KinematicCollision2D collision = MoveAndCollide(velocity * delta);
        if (collision != null)
        {
            var resInf = new CollisionResolutionInfo(collision, this);
            if (!_HandlePlayerCollision(resInf))
            {
                _HandleDefaultCollision(resInf);
            }
            else
            {
                IntersectionTimer.Stop();
                IntersectionTimer.Start();
            }
            VelocityPostProcess(resInf);
        }
        else
        {
            HandleSameDirectionCollision();
        }
    }

    private void HandleSameDirectionCollision()
    {
        if (IntersectionTimer.IsStopped())
        {
            if (IsCollidingWithPlayer())
            {
                bool handled = true;
                if (IsSameDirectionAsPlayer())
                {
                    float s = -Mathf.Sign(Global.Instance().Player.GlobalPosition.x - GlobalPosition.x);
                    velocity.x = s * Mathf.Abs(velocity.x);
                }
                else if (IsPlayerFallingOverTheFallingBall())
                {
                    if (velocity.x > Constants.EPSILON)
                    {
                        velocity = new Vector2(0.0f, -Mathf.Abs(velocity.y));
                    }
                    else
                    {
                        velocity = new Vector2(0.0f, Mathf.Abs(velocity.y)).Normalized() * speed;
                    }
                }
                else if (IsPlayerPushingAFlyingBall())
                {
                    velocity = new Vector2(0.0f, Mathf.Abs(velocity.y)).Normalized() * speed;
                }
                else
                {
                    handled = false;
                }
                if (handled)
                {
                    IntersectionTimer.Stop();
                    IntersectionTimer.Start();
                }
            }
        }
    }

    private bool IsSameDirectionAsPlayer()
    {
        bool sameDirection = player_last_direction * velocity.x >= 0;
        float ratio = RelativeCollisionRatioToSide();
        return sameDirection && ratio < 0.95f && ratio > 0.05f && IsPlayerFollowingTheBall();
    }

    private bool IsPlayerFollowingTheBall()
    {
        var player = Global.Instance().Player;
        if (player.velocity.x > Constants.EPSILON)
        {
            return player.GlobalPosition.x < GlobalPosition.x;
        }
        else if (player.velocity.x < -Constants.EPSILON)
        {
            return player.GlobalPosition.x > GlobalPosition.x;
        }
        else
        {
            return true;
        }
    }

    private bool IsPlayerFallingOverTheFallingBall()
    {
        bool bothFalling = Global.Instance().Player.velocity.y >= 0.0f;
        return bothFalling && IsBallUnderPlayer();
    }

    private bool IsPlayerPushingAFlyingBall()
    {
        bool bothUp = Global.Instance().Player.velocity.y < -Constants.EPSILON && velocity.y < -Constants.EPSILON;
        return bothUp && IsBallOverThePlayer();
    }

    private bool IsBallUnderPlayer()
    {
        var player = Global.Instance().Player;
        var pp = player.GlobalPosition;
        var s = player.GetCollisionShapeSize() * player.Scale;
        var hs = s * 0.5f;
        var bp = GlobalPosition;
        return (pp.y + hs.y) < bp.y && (bp.x > (pp.x - hs.x) && bp.x < (pp.x + hs.x));
    }

    private bool IsBallOverThePlayer()
    {
        var player = Global.Instance().Player;
        var pp = player.GlobalPosition;
        var s = player.GetCollisionShapeSize() * player.Scale;
        var hs = s * 0.5f;
        var bp = GlobalPosition;
        return (pp.y - hs.y) > bp.y && (bp.x > (pp.x - hs.x) && bp.x < (pp.x + hs.x));
    }

    private bool IsCollidingWithPlayer()
    {
        var player = Global.Instance().Player;
        var player_size = player.GetCollisionShapeSize() * player.Scale;
        bool is_idle = player.IsRotationIdle();
        return is_idle && Helpers.Intersects(
            GlobalPosition,
            (AreaCollisionShape.Shape as CircleShape2D).Radius * Scale.x + 10.0f,
            player.GlobalPosition,
            player_size
        );
    }

    private void VelocityPostProcess(CollisionResolutionInfo resInf)
    {
        if (resInf.n.x > Constants.EPSILON)
        {
            velocity.x = Mathf.Sign(resInf.n.x) * Mathf.Abs(velocity.x);
        }
        velocity = velocity.Normalized() * speed;
    }

    private bool IsProbablyABrick(Area2D area, Godot.Collections.Array groups)
    {
        bool is_box_face = Global.Instance().Player.ContainsNode(area);
        return !is_box_face && groups.Count > 0;
    }

    private float RndAngle(float value)
    {
        return (float)GD.RandRange(-value, value);
    }

    private void _on_Area2D_area_entered(Area2D area)
    {
        if (area == DeathZone)
        {
            Event.Instance().EmitBouncingBallRemoved(this);
            QueueFree();
            return;
        }
        var groups = area.GetGroups();
        if (IsProbablyABrick(area, groups))
        {
            if (ColorUtils.COLORS.Contains((string)groups[0]))
            {
                var current_groups = AreaNode.GetGroups();
                foreach (var group in current_groups)
                {
                    AreaNode.RemoveFromGroup((string)group);
                }
                AreaNode.AddToGroup((string)groups[0]);
                color_group = (string)groups[0];
                SpriteNode.SetColor((string)groups[0]);
            }
        }
    }

    private void reset()
    {
        float randomness = (float)GD.RandRange(-SPAWN_DIRECTION_RANDOM_DEGREES, SPAWN_DIRECTION_RANDOM_DEGREES);
        velocity = SPAWN_DIRECTION.Rotated(Mathf.Deg2Rad(randomness)) * SPEED;
    }

    private void SetVelocity(Vector2 _velocity)
    {
        velocity = _velocity;
    }

    public void IncrementSpeed()
    {
        speed += SPEED_UNIT;
        velocity = velocity.Normalized() * speed;
    }

    private void _on_Area2D_body_shape_entered(RID _body_rid, Node body, int body_shape_index, int _local_shape_index)
    {
        if (body != Global.Instance().Player) return;
        // FIXME: fix this dynamic call after c# migration
        body.Call("OnFastAreaCollidingWithPlayerShape", body_shape_index, AreaNode, Constants.EntityType.BALL);
    }

    // Add the missing methods here
    private void SetPlayerLastDirection()
    {
        if (Global.Instance().Player.velocity.x > 0.0f)
        {
            player_last_direction = 1;
        }
        else if (Global.Instance().Player.velocity.x < 0.0f)
        {
            player_last_direction = -1;
        }
    }
}
