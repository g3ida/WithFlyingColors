extends Area2D

export var color_group: String
var is_checked: bool = false
var save_data = {
  "is_checked": false
}

func reset():
  is_checked = save_data["is_checked"]

func save():
  return save_data

func _ready():
  if color_group == null:
    push_error("color_group can not be null")

func _on_Checkpoint_body_shape_entered(_body_rid, _body, _body_shape_index, _local_shape_index):
  if (not is_checked):
    is_checked = true
    save_data["is_checked"] = true
    $CheckHole/CheckDot/AnimationPlayer.play("Checkpoint")
    Event.emit_signal("checkpoint_reached", self)
