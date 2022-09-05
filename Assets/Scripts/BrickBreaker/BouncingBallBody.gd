extends KinematicBody2D

export var color_group: String

onready var AreaNode = $Area2D
onready var SpriteNode = $Sprite

const DEVIATION = 10.0
const SPEED = 7.0
const SPAWN_DIRECTION = Vector2.UP
const SPAWN_DIRECTION_RANDOM_DEGREES = 45.0
const DEVIATION_THRESHOLD = 5.0
const DEVIATION_DEGREES_ADDED = 10.0
const SIDE_COLLISION_NORMAL_THRESHOLD = 0.8
const PLAYER_SIDE_HIT_PUSH_VELOCITY = 200.0

onready var spawn_position: Vector2 = position
var velocity = Vector2.UP * SPEED
var death_zone: Area2D

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

func _physics_process(_delta):
  var collision = move_and_collide(velocity)
  if (collision):
    var side_collision = is_side_collision(collision)
    var n = collision.normal
    var u = velocity.dot(n) * n
    var w = velocity - u
    
    if collision.collider == Global.player and !side_collision:
      var player = collision.collider
      var position = collision.position
      var dp = player.global_position - position
      var player_size = player.get_collision_shape_size()      
      var normalized_pos_x = dp.x / player_size.x
      var m = Vector2(n.y, n.x)
      velocity = DEVIATION * normalized_pos_x*m - u
      velocity = velocity.normalized() * SPEED
    else:
      velocity = w - u
      if side_collision:
        if (Global.player.velocity.x*n.x >= 0):
          Global.player.velocity.x = -n.x * PLAYER_SIDE_HIT_PUSH_VELOCITY
        if (velocity.y > Constants.EPSILON):
          velocity.y = - velocity.y
    
    if collision.collider.is_in_group("wall"):
      var angle_degrees = velocity.angle()*Constants.RAD_TO_DEGREES
      if abs(angle_degrees) < DEVIATION_THRESHOLD or abs(angle_degrees) > 180.0 - DEVIATION_THRESHOLD:
        velocity = velocity.rotated(sign(angle_degrees) * DEVIATION_DEGREES_ADDED * Constants.DEGREES_TO_RAD)
    
func _on_Area2D_area_entered(area):
  if area == death_zone:
    Event.emit_signal("player_died")
  
  var groups = area.get_groups()
  var is_box_face = Global.player.contains_node(area)
  if !is_box_face and groups.size() > 0:
    var current_groups = AreaNode.get_groups()
    for group in current_groups:
      AreaNode.remove_from_group(group)
    AreaNode.add_to_group(groups[0])
    SpriteNode.set_color(groups[0])
    
func reset():
  self.position = spawn_position
  var randomness = rand_range(-SPAWN_DIRECTION_RANDOM_DEGREES, SPAWN_DIRECTION_RANDOM_DEGREES)
  self.velocity = SPAWN_DIRECTION.rotated(randomness*Constants.DEGREES_TO_RAD)*SPEED
