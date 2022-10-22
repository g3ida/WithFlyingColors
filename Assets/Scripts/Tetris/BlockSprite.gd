extends Node2D

export var color_group: String = "blue" setget set_group, get_group

func _ready():
  pass

func set_group(group: String):
  color_group = group
  var color_index = ColorUtils.get_group_color_index(color_group)
  var base_color = ColorUtils.get_basic_color(color_index)
  var light_color = ColorUtils.get_light_color(color_index)
  var dark_color = ColorUtils.get_dark_color(color_index)
  #apply colors
  $Layer1.modulate = light_color
  $Layer2.modulate = dark_color
  $TopLayer.modulate = base_color

func get_group():
  return color_group
