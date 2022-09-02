extends Area2D

onready var collisionShapeNode = $CollisionShape2D

var width: float
var height:float

func _ready():
  width = collisionShapeNode.shape.extents.x
  height = collisionShapeNode.shape.extents.y
