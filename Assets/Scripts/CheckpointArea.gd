extends Area2D

signal playerEntred()

func _ready():
  pass
      
func _on_CheckpointArea_body_entered(_body):
  emit_signal("playerEntred")
  set_camera_limits()

func set_camera_limits():
  for ch in get_children():
    if ch.name == "BottomLeft":
      Global.camera.limit_left = ch.global_position.x
      Global.camera.limit_bottom = ch.global_position.y
    elif ch.name == "TopRight":
      Global.camera.limit_right = ch.global_position.x
      Global.camera.limit_top = ch.global_position.y
