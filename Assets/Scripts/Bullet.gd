extends Node2D

const SPEED = 10.0 * Global.WORLD_TO_SCREEN
const MAX_DISTANCE = 5000.0
const MAX_DISTANCE_SQUARED = MAX_DISTANCE * MAX_DISTANCE

onready var bodyNode = $KinematicBody2D
onready var spriteNode = $KinematicBody2D/BulletSpr
onready var ColorAreaNode = $KinematicBody2D/ColorArea

var gravity = 1.0 * Global.WORLD_TO_SCREEN
var movement = Vector2()
var initial_position = Vector2()

func _ready():
  pass
  
func shoot(shoot_direction: Vector2):
  movement = shoot_direction * SPEED
  
func set_color_group(group_name: String):
  ColorAreaNode.add_to_group(group_name)
  var color_index = ColorUtils.get_group_color_index(group_name)
  spriteNode.modulate = ColorUtils.get_basic_color(color_index)
  
func _physics_process(delta):
  movement.y += delta * gravity
  bodyNode.move_and_slide(movement)
  
  if (global_position - initial_position).length_squared() > MAX_DISTANCE_SQUARED:
    queue_free()

func _on_ColorArea_body_entered(_body):
    queue_free()

func _on_ColorArea_body_shape_entered(_body_rid, body, body_shape_index, _local_shape_index):
  if body != Global.player: return
  body.on_fast_area_colliding_with_player_shape(body_shape_index, ColorAreaNode, Global.EntityType.BULLET)
