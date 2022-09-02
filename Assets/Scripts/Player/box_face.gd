extends BaseFace

func _ready():
  pass

func scale_by(factor: float):
  var scale_factor: float = factor
  self.scale = Vector2(scale_factor, scale_factor)

  self.position.x = position_x + extents.y * (scale_factor - 1.0) * Helpers.sign_of(self.position.y) * sin(rotation)
  self.position.y = position_y - extents.y * (scale_factor - 1.0) * Helpers.sign_of(self.position.y) * cos(rotation)

func _on_bottomFace_area_entered(area):
  var group = get_groups()
  assert(group.size() == 1)
  
  if area.is_in_group("fallzone"):
    Event.emit_signal("player_diying", null, global_position, Global.EntityType.FALLZONE)
  
  if not (area.is_in_group(group.front())):
    Event.emit_signal("player_diying", area, global_position, Global.EntityType.PLATFORM)
  else:
    Event.emit_signal("player_landed", area, global_position)
