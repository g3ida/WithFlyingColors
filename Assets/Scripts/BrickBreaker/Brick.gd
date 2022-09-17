tool
extends Node2D

signal brick_broken()

export var color_group: String = "blue"

onready var AreaNode = $Area2D
onready var SpriteNode = $Sprite
onready var CollisionShapeNode = $KinematicBody2D/CollisionShape2D

func _set_color():
  var color = ColorUtils.get_color(color_group)
  SpriteNode.modulate = color

func _ready():
  AreaNode.add_to_group(color_group)
  _set_color()

func _on_Area2D_area_entered(_area):
  var extents = CollisionShapeNode.shape.extents
  emit_signal("brick_broken")
  Event.emit_signal("brick_broken", color_group, position + get_parent().position + extents)
  queue_free()
