class_name BaseFace
extends Area2D

onready var collisionShapeNode = $CollisionShape2D
onready var extents: Vector2 = collisionShapeNode.shape.extents

onready var position_x := self.position.x
onready var position_y := self.position.y

func check_group(area: Area2D, groups: Array) -> bool:
  for group in groups:
    if area.is_in_group(group):
      return true
  return false

func scale_by(factor: float):
  var scale_factor: float = factor
  self.scale = Vector2(scale_factor, scale_factor)
  self.position.x = position_x - extents.x * (scale_factor - 1.0) * Helpers.sign_of(self.position.x)
  self.position.y = position_y - extents.y * (scale_factor - 1.0) * Helpers.sign_of(self.position.y)
