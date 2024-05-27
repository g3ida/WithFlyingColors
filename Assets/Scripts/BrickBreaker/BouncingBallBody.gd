class_name BouncingBall
extends KinematicBody2D

export var color_group: String = "blue"

onready var AreaNode = $Area2D
onready var SpriteNode = $BBSpr  
onready var AreaCollisionShape = $Area2D/CollisionShape2D
onready var IntersectionTimer = $IntersectionTimer

const DEVIATION = 10.0
const SPEED = 420.0
const SPAWN_DIRECTION = Vector2.UP
const SPAWN_DIRECTION_RANDOM_DEGREES = 45.0
const DEVIATION_THRESHOLD = 5.0
const DEVIATION_DEGREES_ADDED = 10.0
const SIDE_COLLISION_NORMAL_THRESHOLD = 0.8
const PLAYER_SIDE_HIT_PUSH_VELOCITY = 250.0
const SPEED_UNIT = 60.0

onready var spawn_position: Vector2 = position
var velocity = Vector2.UP * SPEED
var death_zone: Area2D
var speed = SPEED
var player_last_direction = 0

class CollisionResolutionInfo:
  func _init(_collision: KinematicCollision2D, ball: KinematicBody2D):
    self.collision = _collision
    self.angle = ball.velocity.angle()
    self.angle_deg = rad2deg(self.angle)
    self.player = Global.player
    self.is_player = Global.player == collision.collider
    self.is_wall = collision.collider.is_in_group("wall")
    self.is_side_col = ball.is_side_collision(_collision)
    self.side_ratio = -1 if  !self.is_side_col else ball.relative_collision_ratio_to_side()
    self.n = collision.normal
    self.u = ball.velocity.dot(n) * n
    self.w = ball.velocity - u
    self.side = sign(player.global_position.x - collision.position.x)
  var collision: KinematicCollision2D
  var angle: float # the angle of the ball in rad
  var angle_deg: float # the angle of the ball in degrees
  var player: KinematicBody2D #the player object
  var is_player: bool # if the collided with object is the player
  var is_wall: bool # if the collided with object is wall
  var is_side_col: bool # whether the ball touched the player from its side
  var side_ratio: float # how much is the collision position in the side (1.0 top side, 0.0 bottom side)
  var side: float # which side does it touch the player
  var n # collision normal
  var u; var w;
  
func _ready():
  randomize()
  reset()
  set_color(color_group)
  
func set_color(color_name):
  SpriteNode.set_color(color_name)
  if AreaNode.is_in_group(color_group):
    AreaNode.remove_from_group(color_group)
  AreaNode.add_to_group(color_name)
  color_group = color_name

func is_side_collision(collision: KinematicCollision2D):
  if collision.collider != Global.player:
    return false
  return abs(collision.normal.y) < SIDE_COLLISION_NORMAL_THRESHOLD

func relative_collision_ratio_to_side():
  var scale_y = Global.player.scale.y
  var ppos = Global.player.global_position
  var dims = Global.player.get_collision_shape_size()
  var ratio = (ppos.y+dims.y*scale_y*0.5 - global_position.y) / dims.y * scale_y
  return clamp(ratio, 0.0, 1.0)

func is_falling_stright_and_colliding_with_side(side_collision, angle_degrees):
  return side_collision and velocity.y > Constants.EPSILON and abs(abs(angle_degrees) - 90.0) < 45.0
  
func _handle_player_collision(info: CollisionResolutionInfo):
  if info.is_player:
    if !info.is_side_col:
      if (_is_ball_under_player()):
        velocity = Vector2(0, 1)
      else:
        var position = info.collision.position
        var dp = info.player.global_position - position
        var player_size = info.player.get_collision_shape_size()
        var normalized_pos_x = dp.x / player_size.x
        var m = Vector2(info.n.y, info.n.x)
        velocity = DEVIATION * SPEED_UNIT * normalized_pos_x*m - info.u
    else: # side collision
      if is_falling_stright_and_colliding_with_side(info.is_side_col, info.angle_deg):
        velocity = Vector2(info.side, 0).rotated(-info.side*deg2rad(rand_range(0.0, 5.0)))
      else:
        velocity = info.w - info.u
        if velocity.y > Constants.EPSILON: #check ratio
          velocity.y = -velocity.y
      # avoid player sticking to the ball
      if info.player.velocity.x*info.n.x >= 0:
        info.player.velocity.x = -info.n.x * PLAYER_SIDE_HIT_PUSH_VELOCITY
    return true
  return false

func _is_vertical_wall(info: CollisionResolutionInfo):
  return abs(info.n.x) > 0.5 and abs(info.n.y) < Constants.EPSILON

func _is_ball_almost_horizontal(info: CollisionResolutionInfo):
  return info.angle_deg < DEVIATION_THRESHOLD or abs(info.angle_deg) > 180.0 - DEVIATION_THRESHOLD

func _is_ball_almost_vertical():
  return abs(Vector2.UP.angle_to(velocity)) < DEVIATION_THRESHOLD or abs(Vector2.DOWN.angle_to(velocity)) < DEVIATION_THRESHOLD

func _is_horizontal_wall(info: CollisionResolutionInfo):
  return abs(info.n.y) > 0.5 and abs(info.n.x) < Constants.EPSILON

func _handle_default_collision(info: CollisionResolutionInfo):
  velocity = info.w - info.u
  if info.is_wall:
    if _is_vertical_wall(info) and _is_ball_almost_horizontal(info):
      velocity = velocity.rotated(sign(velocity.y*info.n.x)* deg2rad(rand_range(0, DEVIATION_DEGREES_ADDED)))
    elif _is_horizontal_wall(info) and _is_ball_almost_vertical():
      velocity = velocity.rotated(-sign(velocity.x)*deg2rad(rand_range(0, DEVIATION_DEGREES_ADDED)))

func _set_player_last_direction():
  if Global.player.velocity.x > 0.0:
    player_last_direction = 1
  elif Global.player.velocity.x < 0.0:
    player_last_direction = -1

func _physics_process(_delta):
  _set_player_last_direction()
  var collision = move_and_collide(velocity * _delta)
  if (collision):
    var res_inf = CollisionResolutionInfo.new(collision, self)
    if not _handle_player_collision(res_inf):
      _handle_default_collision(res_inf)
    else:
      IntersectionTimer.stop()
      IntersectionTimer.start()
    _velocity_post_process(res_inf)
  else:
    # the player move_and_slide if he gets in collision with the ball it is not detected because
    # he won't slide until there is a collision so here we are taking care of this case 
    _handle_same_direction_collision()

func _handle_same_direction_collision():
  if IntersectionTimer.is_stopped():
    if _is_colliding_with_player():
      var handled = true
      if _is_same_direction_as_player():
        var s = -sign(global_position.x - Global.player.global_position.x)
        velocity.x = s* abs(velocity.x)
      elif _is_player_falling_over_the_falling_ball():
        if velocity.x > Constants.EPSILON:
          velocity = Vector2(0.0, -abs(velocity.y))
        else:
          velocity = Vector2(0.0, abs(velocity.y)).normalized() * speed
      elif _is_player_pushing_a_flying_ball():
        velocity = Vector2(0.0, abs(velocity.y)).normalized() * speed
      else:
        handled = false
      if handled:
        IntersectionTimer.stop()
        IntersectionTimer.start()
    
func _is_same_direction_as_player():
  var same_direction = player_last_direction * velocity.x >= 0
  var ratio = relative_collision_ratio_to_side()
  return same_direction and ratio < 0.95 and ratio > 0.05 and _is_player_following_the_ball()

func _is_player_following_the_ball():
  var player = Global.player
  if player.velocity.x > Constants.EPSILON:
    return player.global_position.x < global_position.x
  elif  player.velocity.x < -Constants.EPSILON:
    return player.global_position.x > global_position.x
  else:
    return true

func _is_player_falling_over_the_falling_ball():
  var both_falling = Global.player.velocity.y >= 0.0
  return both_falling and _is_ball_under_player()
  
func _is_player_pushing_a_flying_ball():
  var both_up = Global.player.velocity.y < -Constants.EPSILON and velocity.y < -Constants.EPSILON
  return both_up and _is_ball_over_the_player()

 
func _is_ball_under_player():
  var player = Global.player
  var pp = player.global_position
  var s = player.get_collision_shape_size() * player.scale
  var hs = s * 0.5
  var bp = global_position
  return ((pp.y+hs.y) < bp.y) and (bp.x > (pp.x-hs.x) and bp.x < (pp.x+hs.x))

func _is_ball_over_the_player():
  var player = Global.player
  var pp = player.global_position
  var s = player.get_collision_shape_size() * player.scale
  var hs = s * 0.5
  var bp = global_position
  return ((pp.y-hs.y) > bp.y) and (bp.x > (pp.x-hs.x) and bp.x < (pp.x+hs.x))
  
func _is_colliding_with_player():
  var player = Global.player
  var player_size = player.get_collision_shape_size() * player.scale
  var is_idle = player.player_rotation_state.base_state == PlayerStatesEnum.IDLE
  return is_idle and Helpers.intersects(
    global_position,
    AreaCollisionShape.shape.radius*scale.x + 10.0, # adding some offset for collision
    player.global_position,
    player_size)

func _velocity_post_process(res_inf: CollisionResolutionInfo):
  if res_inf.n.x > Constants.EPSILON:
    velocity.x = sign(res_inf.n.x) * abs(velocity.x)
  velocity = velocity.normalized() * speed

func is_probably_a_brick(area, groups):
  var is_box_face = Global.player.contains_node(area)
  return !is_box_face and groups.size() > 0

func rnd_angle(value: float) -> float:
  return rand_range(-value, value)

func _on_Area2D_area_entered(area):
  if area == death_zone:
    Event.emit_signal("bouncing_ball_removed", self)
    queue_free()
    return
  var groups = area.get_groups()
  if is_probably_a_brick(area, groups):
    if groups[0] in ColorUtils.COLORS:
      var current_groups = AreaNode.get_groups()
      for group in current_groups:
        AreaNode.remove_from_group(group)
      AreaNode.add_to_group(groups[0])
      color_group = groups[0]
      SpriteNode.set_color(groups[0])
    
func reset():
  self.position = spawn_position
  var randomness = rand_range(-SPAWN_DIRECTION_RANDOM_DEGREES, SPAWN_DIRECTION_RANDOM_DEGREES)
  self.velocity = SPAWN_DIRECTION.rotated(deg2rad(randomness))*SPEED

func set_velocity(_velocity: Vector2):
  self.velocity = _velocity

func increment_speed():
  speed = speed + SPEED_UNIT
  self.velocity = self.velocity.normalized() * speed

func _on_Area2D_body_shape_entered(_body_rid:RID, body:Node, body_shape_index:int, _local_shape_index:int):
  if body != Global.player: return
  body.on_fast_area_colliding_with_player_shape(body_shape_index, AreaNode, Constants.EntityType.BALL)
