extends Area2D

# Called when the node enters the scene tree for the first time.
func _ready():
  pass # Replace with function body.

func _on_FallzoneArea_area_entered(area):
  Event.emit_signal("player_diying", null, area.global_position)
