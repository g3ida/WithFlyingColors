extends Area2D

func _on_bottomFace_area_entered(area):
  var group = get_groups()
  assert(group.size() == 1)
  if not (area.is_in_group(group.front())):
    Event.emit_signal("player_diying", area, global_position)
  else:
    Event.emit_signal("player_landed", area, global_position)
