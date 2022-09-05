tool
extends Node2D

export var color_group: String = "blue"
export var sprite: Texture

onready var AreaNode = $Area2D
onready var SpriteNode = $Sprite

func _ready():
  AreaNode.add_to_group(color_group)
  if sprite != null:
    SpriteNode.texture = sprite

func _on_Area2D_area_entered(_area):
  queue_free()
