extends BaseFace

func _ready():
  pass

func _on_area_entered(area: Area2D):
  if area.is_in_group("fallzone"):
      Event.emit_signal("player_diying", null, global_position, Global.EntityType.FALLZONE)
      
  var groups = get_groups()
  if not (check_group(area, groups)):
    Event.emit_signal("player_diying", area, global_position, Global.EntityType.PLATFORM)
  else:
    Event.emit_signal("player_landed", area, global_position)
