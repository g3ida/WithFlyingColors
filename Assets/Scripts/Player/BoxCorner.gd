extends BaseFace

onready var CollisionShapeNode = $CollisionShape2D
onready var edge_length = CollisionShapeNode.shape.extents.x

func _ready():
  pass

func _on_area_entered(area: Area2D):
  if area.is_in_group("fallzone"):
      Event.emit_signal("player_diying", null, global_position, Constants.EntityType.FALLZONE)
      return
  var groups = get_groups()
  if not (check_group(area, groups)):
    Event.emit_signal("player_diying", area, global_position, Constants.EntityType.PLATFORM)
  elif area is Gem:
    (area as Gem)._on_Gem_area_entered(self)  
  elif Global.player.player_state.base_state != PlayerStatesEnum.STANDING:
    Event.emit_signal("player_landed", area, global_position)
