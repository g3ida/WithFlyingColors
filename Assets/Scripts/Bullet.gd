extends Node2D

const SPEED = 10.0 * Global.WORLD_TO_SCREEN
const MAX_DISTANCE = 5000.0
const MAX_DISTANCE_SQUARED = MAX_DISTANCE * MAX_DISTANCE

onready var bodyNode = $KinematicBody2D
onready var spriteNode = $KinematicBody2D/Sprite
onready var ColorAreaNode = $KinematicBody2D/ColorArea

var gravity = 1.0 * Global.WORLD_TO_SCREEN
var movement = Vector2()
var initial_position = Vector2()

func _ready():
  pass
  
func set_texture(texture: Texture):
  spriteNode.texture = texture
  
func shoot(shoot_direction: Vector2):
  movement = shoot_direction * SPEED
  
func set_color_group(group_name: String):
  ColorAreaNode.add_to_group(group_name)
  
func _physics_process(delta):
  movement.y += delta * gravity
  bodyNode.move_and_slide(movement)
  
  if (global_position - initial_position).length_squared() > MAX_DISTANCE_SQUARED:
    queue_free()

func _on_ColorArea_body_entered(_body):
    queue_free()
