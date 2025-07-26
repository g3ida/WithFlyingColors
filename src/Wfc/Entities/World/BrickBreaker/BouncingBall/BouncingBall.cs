namespace Wfc.Entities.World.BrickBreaker;

using System;
using System.Linq;
using Godot;
using Wfc.Entities.World;
using Wfc.Entities.World.Player;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
public partial class BouncingBall : CharacterBody2D {
  #region Exported
  [Export]
  public string ColorGroup = ColorUtils.BLUE;
  #endregion Exported

  #region Constants
  private const float DEVIATION = 10.0f;
  private const float SPEED = 420.0f;
  private static readonly Vector2 SPAWN_DIRECTION = Vector2.Up;
  private const float SPAWN_DIRECTION_RANDOM_DEGREES = 45.0f;
  private const float DEVIATION_THRESHOLD = 5.0f;
  private const float DEVIATION_DEGREES_ADDED = 10.0f;
  private const float PLAYER_SIDE_HIT_PUSH_VELOCITY = 250.0f;
  private const float SPEED_UNIT = 60.0f;
  #endregion Constants

  #region Nodes
  [NodePath("Area2D")]
  private Area2D _areaNode = default!;
  [NodePath("BBSpr")]
  private BouncingBallSprite _spriteNode = default!;
  [NodePath("Area2D/CollisionShape2D")]
  private CollisionShape2D _areaCollisionShape = default!;
  [NodePath("IntersectionTimer")]
  private Timer _intersectionTimer = default!;
  #endregion Nodes

  public Vector2 BallVelocity { get => _ballVelocity; set => _ballVelocity = value; }
  private Vector2 _ballVelocity = Vector2.Up * SPEED;

  private float _speed = SPEED;
  public Area2D? DeathZone = null; // FIXME: this is set in breakBreaker. better logic ?
  private int _playerLastDirection;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    GD.Randomize();
    reset();
    SetColor(ColorGroup);
  }

  public void SetColor(string colorName) {
    _spriteNode.SetColor(colorName);
    if (_areaNode.IsInGroup(ColorGroup))
      _areaNode.RemoveFromGroup(ColorGroup);
    _areaNode.AddToGroup(colorName);
    ColorGroup = colorName;
  }


  public float RelativeCollisionRatioToSide() {
    var scale_y = Global.Instance().Player.Scale.Y;
    var pos = Global.Instance().GlobalPosition;
    var dims = Global.Instance().Player.GetCollisionShapeSize();
    var ratio = (pos.Y + (dims.Y * scale_y * 0.5f) - GlobalPosition.Y) / dims.Y * scale_y;
    return Mathf.Clamp(ratio, 0.0f, 1.0f);
  }

  private bool IsFallingStraightAndCollidingWithSide(bool sideCollision, float angleDegrees) {
    return sideCollision && _ballVelocity.Y > MathUtils.EPSILON && Math.Abs(Math.Abs(angleDegrees) - 90.0f) < 45.0f;
  }

  private bool _HandlePlayerCollision(CollisionResolutionInfo info) {
    if (info.IsPlayer && info.Player is Player player) {
      if (!info.IsSideCol) {
        if (IsBallUnderPlayer())
          _ballVelocity = new Vector2(0, 1);
        else {
          var position = info.Collision.GetPosition();
          var dp = player.GlobalPosition - position;
          var playerSize = (player as Player).GetCollisionShapeSize();
          var normalizedPosX = dp.X / playerSize.X;
          var m = new Vector2(info.N.Y, info.N.X);
          _ballVelocity = DEVIATION * SPEED_UNIT * normalizedPosX * m - info.U;
        }
      }
      else // side collision
      {
        if (IsFallingStraightAndCollidingWithSide(info.IsSideCol, info.AngleDeg))
          _ballVelocity = new Vector2(info.Side, 0).Rotated(-info.Side * Mathf.DegToRad((float)GD.RandRange(0.0f, 5.0f)));
        else {
          _ballVelocity = info.W - info.U;
          if (_ballVelocity.Y > MathUtils.EPSILON) //check ratio
            _ballVelocity.Y = -_ballVelocity.Y;
        }
        // avoid player sticking to the ball
        if (player.Velocity.X * info.N.X >= 0)
          player.Velocity = new Vector2(-info.N.X * PLAYER_SIDE_HIT_PUSH_VELOCITY, player.Velocity.Y);
      }
      return true;
    }
    return false;
  }

  private static bool _IsVerticalWall(CollisionResolutionInfo info) {
    return Mathf.Abs(info.N.X) > 0.5f && Mathf.Abs(info.N.Y) < MathUtils.EPSILON;
  }

  private static bool _IsBallAlmostHorizontal(CollisionResolutionInfo info) {
    return info.AngleDeg < DEVIATION_THRESHOLD || Mathf.Abs(info.AngleDeg) > 180.0f - DEVIATION_THRESHOLD;
  }

  private bool _IsBallAlmostVertical() {
    return Mathf.Abs(Vector2.Up.AngleTo(_ballVelocity)) < DEVIATION_THRESHOLD || Mathf.Abs(Vector2.Down.AngleTo(_ballVelocity)) < DEVIATION_THRESHOLD;
  }

  private static bool _IsHorizontalWall(CollisionResolutionInfo info) {
    return Mathf.Abs(info.N.Y) > 0.5f && Mathf.Abs(info.N.X) < MathUtils.EPSILON;
  }

  private void _HandleDefaultCollision(CollisionResolutionInfo info) {
    _ballVelocity = info.W - info.U;
    if (info.IsWall) {
      if (_IsVerticalWall(info) && _IsBallAlmostHorizontal(info))
        _ballVelocity = BallVelocity.Rotated(Math.Sign(_ballVelocity.Y * info.N.X) * Mathf.DegToRad((float)GD.RandRange(0, DEVIATION_DEGREES_ADDED)));
      else if (_IsHorizontalWall(info) && _IsBallAlmostVertical())
        _ballVelocity = BallVelocity.Rotated(-Math.Sign(_ballVelocity.X) * Mathf.DegToRad((float)GD.RandRange(0, DEVIATION_DEGREES_ADDED)));
    }
  }

  private void _SetPlayerLastDirection() {
    if (Global.Instance().Player.Velocity.X > 0.0f)
      _playerLastDirection = 1;
    else if (Global.Instance().Player.Velocity.X < 0.0f)
      _playerLastDirection = -1;
  }

  public override void _PhysicsProcess(double delta) {
    SetPlayerLastDirection();
    KinematicCollision2D collision = MoveAndCollide(_ballVelocity * (float)delta);
    if (collision != null) {
      var resInf = new CollisionResolutionInfo(collision, this);
      if (!_HandlePlayerCollision(resInf)) {
        _HandleDefaultCollision(resInf);
      }
      else {
        _intersectionTimer.Stop();
        _intersectionTimer.Start();
      }
      VelocityPostProcess(resInf);
    }
    else {
      HandleSameDirectionCollision();
    }
  }

  private void HandleSameDirectionCollision() {
    if (_intersectionTimer.IsStopped()) {
      if (IsCollidingWithPlayer()) {
        bool handled = true;
        if (IsSameDirectionAsPlayer()) {
          float s = -Mathf.Sign(Global.Instance().Player.GlobalPosition.X - GlobalPosition.X);
          _ballVelocity.X = s * Mathf.Abs(BallVelocity.X);
        }
        else if (IsPlayerFallingOverTheFallingBall()) {
          if (_ballVelocity.X > MathUtils.EPSILON) {
            _ballVelocity = new Vector2(0.0f, -Mathf.Abs(_ballVelocity.Y));
          }
          else {
            _ballVelocity = new Vector2(0.0f, Mathf.Abs(_ballVelocity.Y)).Normalized() * _speed;
          }
        }
        else if (IsPlayerPushingAFlyingBall()) {
          _ballVelocity = new Vector2(0.0f, Mathf.Abs(_ballVelocity.Y)).Normalized() * _speed;
        }
        else {
          handled = false;
        }
        if (handled) {
          _intersectionTimer.Stop();
          _intersectionTimer.Start();
        }
      }
    }
  }

  private bool IsSameDirectionAsPlayer() {
    bool sameDirection = _playerLastDirection * _ballVelocity.X >= 0;
    float ratio = RelativeCollisionRatioToSide();
    return sameDirection && ratio < 0.95f && ratio > 0.05f && IsPlayerFollowingTheBall();
  }

  private bool IsPlayerFollowingTheBall() {
    var player = Global.Instance().Player;
    if (player.Velocity.X > MathUtils.EPSILON) {
      return player.GlobalPosition.X < GlobalPosition.X;
    }
    else if (player.Velocity.X < -MathUtils.EPSILON) {
      return player.GlobalPosition.X > GlobalPosition.X;
    }
    else {
      return true;
    }
  }

  private bool IsPlayerFallingOverTheFallingBall() {
    bool bothFalling = Global.Instance().Player.Velocity.Y >= 0.0f;
    return bothFalling && IsBallUnderPlayer();
  }

  private bool IsPlayerPushingAFlyingBall() {
    bool bothUp = Global.Instance().Player.Velocity.Y < -MathUtils.EPSILON && _ballVelocity.Y < -MathUtils.EPSILON;
    return bothUp && IsBallOverThePlayer();
  }

  private bool IsBallUnderPlayer() {
    var player = Global.Instance().Player;
    var pp = player.GlobalPosition;
    var s = player.GetCollisionShapeSize() * player.Scale;
    var hs = s * 0.5f;
    var bp = GlobalPosition;
    return (pp.Y + hs.Y) < bp.Y && (bp.X > (pp.X - hs.X) && bp.X < (pp.X + hs.X));
  }

  private bool IsBallOverThePlayer() {
    var player = Global.Instance().Player;
    var pp = player.GlobalPosition;
    var s = player.GetCollisionShapeSize() * player.Scale;
    var hs = s * 0.5f;
    var bp = GlobalPosition;
    return (pp.Y - hs.Y) > bp.Y && (bp.X > (pp.X - hs.X) && bp.X < (pp.X + hs.X));
  }

  private bool IsCollidingWithPlayer() {
    var player = Global.Instance().Player;
    var playerSize = player.GetCollisionShapeSize() * player.Scale;
    bool isIdle = player.IsRotationIdle();
    if (_areaCollisionShape.Shape is CircleShape2D circleShape) {
      return isIdle && GeometryHelpers.Intersects(
              GlobalPosition,
              circleShape.Radius * Scale.X + 10.0f,
              player.GlobalPosition,
              playerSize
          );
    }
    else {
      return isIdle;
    }
  }

  private void VelocityPostProcess(CollisionResolutionInfo resInf) {
    if (resInf.N.X > MathUtils.EPSILON) {
      _ballVelocity.X = Mathf.Sign(resInf.N.X) * Mathf.Abs(_ballVelocity.X);
    }
    _ballVelocity = _ballVelocity.Normalized() * _speed;
  }

  private static bool IsProbablyABrick(Area2D area, Godot.Collections.Array<StringName> groups) {
    bool is_box_face = Global.Instance().Player.ContainsNode(area);
    return !is_box_face && groups.Count > 0;
  }

  private static float RndAngle(float value) {
    return (float)GD.RandRange(-value, value);
  }

  private void _on_Area2D_area_entered(Area2D area) {
    if (area == DeathZone) {
      EventHandler.Instance.EmitBouncingBallRemoved(this);
      QueueFree();
      return;
    }
    var groups = area.GetGroups();
    if (IsProbablyABrick(area, groups)) {
      if (ColorUtils.COLOR_GROUPS.Contains((string)groups[0])) {
        var current_groups = _areaNode.GetGroups();
        foreach (var group in current_groups) {
          _areaNode.RemoveFromGroup((string)group);
        }
        _areaNode.AddToGroup((string)groups[0]);
        ColorGroup = (string)groups[0];
        _spriteNode.SetColor((string)groups[0]);
      }
    }
  }

  private void reset() {
    float randomness = (float)GD.RandRange(-SPAWN_DIRECTION_RANDOM_DEGREES, SPAWN_DIRECTION_RANDOM_DEGREES);
    _ballVelocity = SPAWN_DIRECTION.Rotated(Mathf.DegToRad(randomness)) * SPEED;
  }

  public void IncrementSpeed() {
    _speed += SPEED_UNIT;
    _ballVelocity = _ballVelocity.Normalized() * _speed;
  }

  private void _onArea2DBodyShapeEntered(Rid bodyRid, Node body, int bodyShapeIndex, int localShapeIndex) {
    if (body is Player player) {
      player.OnFastAreaCollidingWithPlayerShape((uint)bodyShapeIndex, _areaNode, EntityType.Ball);
    }
  }

  // Add the missing methods here
  private void SetPlayerLastDirection() {
    if (Global.Instance().Player.Velocity.X > 0.0f) {
      _playerLastDirection = 1;
    }
    else if (Global.Instance().Player.Velocity.X < 0.0f) {
      _playerLastDirection = -1;
    }
  }
}
