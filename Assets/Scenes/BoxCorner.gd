extends Area2D

func check_group(area: Area2D, groups: Array) -> bool:
  for group in groups:
    if area.is_in_group(group):
      return true
  return false

func _on_area_entered(area: Area2D):

  if area.is_in_group("fallzone"):
      Event.emit_signal("player_diying", null, global_position)
      
  var groups = get_groups()
  if not (check_group(area, groups)):
    Event.emit_signal("player_diying", area, global_position)
  else:
    Event.emit_signal("player_landed", area, global_position)
