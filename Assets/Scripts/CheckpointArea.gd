extends Area2D

signal checkpoint_hit()

export var color_group: String
var is_checked: bool = false

func _ready():
  if color_group == null:
    push_error("color_group can not be null")
  
      
func _on_CheckpointArea_body_entered(_body):
  if not is_checked:
    is_checked = true
    set_camera_limits()
    emit_signal("checkpoint_hit")
    Event.emit_signal("checkpoint_reached", self)

func set_camera_limits():
  for ch in get_children():
    if ch.name == "BottomLeft":
      Global.camera.limit_left = ch.global_position.x
      Global.camera.limit_bottom = ch.global_position.y
    elif ch.name == "TopRight":
      Global.camera.limit_right = ch.global_position.x
      Global.camera.limit_top = ch.global_position.y
