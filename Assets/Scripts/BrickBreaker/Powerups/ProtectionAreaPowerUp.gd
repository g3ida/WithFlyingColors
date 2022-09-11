extends PowerUpScript

const ProtectionArea = preload("res://Assets/Scenes/BrickBreaker/Powerups/ProtectionArea.tscn")

var is_relevant = true
var protection_area: Node2D

func _ready():
  set_process(false)
  protection_area = ProtectionArea.instance()
  protection_area.position = BrickBreakerNode.ProtectionAreaSpawnerPositionNode.position
  BrickBreakerNode.call_deferred("add_child", protection_area)
  protection_area.call_deferred("set_owner", BrickBreakerNode)
  var __ = protection_area.connect("tree_exited", self, "_on_protection_area_destoryed")

func _on_protection_area_destoryed():
  is_relevant = false

func _exit_tree():
  if is_still_relevant() and protection_area != null:
    protection_area.queue_free()

func is_still_relevant():
  return is_relevant
