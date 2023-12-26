tool
class_name Brick
extends Node2D

signal brick_broken()

export var color_group: String = "blue"

onready var AreaNode = $Area2D
onready var SpriteNode = $BrickSpr
onready var CollisionShapeNode = $KinematicBody2D/CollisionShape2D

func _set_color():
  var color_index = ColorUtils.get_group_color_index(color_group)
  var color = ColorUtils.get_basic_color(color_index)
  SpriteNode.modulate = color

func _ready():
  AreaNode.add_to_group(color_group)
  _set_color()

func _on_Area2D_area_entered(_area):
  var extents = CollisionShapeNode.shape.extents
  emit_signal("brick_broken")
  Event.emit_signal("brick_broken", color_group, position + get_parent().position + extents)
  queue_free()
