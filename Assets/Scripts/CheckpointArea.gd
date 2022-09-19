class_name CheckpointArea
extends Area2D

signal checkpoint_hit()

export var color_group: String
export var full_viewport_drag_margin = false
var is_checked: bool = false

func _ready():
  if color_group == null:
    push_error("color_group can not be null")
  
func _on_CheckpointArea_body_entered(_body):
  if _body == Global.player and not is_checked:
      is_checked = true
      emit_signal("checkpoint_hit")
      Event.emit_signal("checkpoint_reached", self)
