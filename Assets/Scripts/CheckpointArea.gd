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
      set_camera_limits()
      set_camera_drag_margins()
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
    
func set_camera_drag_margins():
  if full_viewport_drag_margin:
    Global.camera.set_drag_margin_bottom(1)
    Global.camera.set_drag_margin_left(1)
    Global.camera.set_drag_margin_right(1)
    Global.camera.set_drag_margin_top(1)
  else:
    #to do in a constant
    Global.camera.set_drag_margin_bottom(Constants.DEFAULT_DRAG_MARGIN_TB)
    Global.camera.set_drag_margin_left(Constants.DEFAULT_DRAG_MARGIN_LR)
    Global.camera.set_drag_margin_right(Constants.DEFAULT_DRAG_MARGIN_LR)
    Global.camera.set_drag_margin_top(Constants.DEFAULT_DRAG_MARGIN_TB)
