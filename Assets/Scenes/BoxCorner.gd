extends Area2D

onready var collisionShapeNode = $CollisionShape2D
var extents: Vector2

onready var position_x := self.position.x
onready var position_y := self.position.y

func _ready():
  extents = collisionShapeNode.shape.extents

func check_group(area: Area2D, groups: Array) -> bool:
  for group in groups:
    if area.is_in_group(group):
      return true
  return false
  
func sign_of(x: float) -> float: return -1.0 if x < 0 else 1.0
  
func scale_by(factor: float):
  var scale_factor: float = factor
  self.scale = Vector2(scale_factor, scale_factor)
  self.position.x = position_x - extents.x * (scale_factor - 1) * sign_of(self.position.x)
  self.position.y = position_y - extents.y * (scale_factor - 1) * sign_of(self.position.y)
  
func _on_area_entered(area: Area2D):
  if area.is_in_group("fallzone"):
      Event.emit_signal("player_diying", null, global_position, Global.EntityType.FALLZONE)
      
  var groups = get_groups()
  if not (check_group(area, groups)):
    Event.emit_signal("player_diying", area, global_position, Global.EntityType.PLATFORM)
  else:
    Event.emit_signal("player_landed", area, global_position)
