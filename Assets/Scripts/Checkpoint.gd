extends Area2D

export var color_group: String
var is_checked: bool = false

func _ready():
  if color_group == null:
    push_error("color_group can not be null")

func _on_Checkpoint_body_shape_entered(body_rid, body, body_shape_index, local_shape_index):
  if (not is_checked):
    is_checked = true
    $CheckHole/CheckDot/AnimationPlayer.play("Checkpoint")
    Event.emit_signal("checkpoint_reached", self)
