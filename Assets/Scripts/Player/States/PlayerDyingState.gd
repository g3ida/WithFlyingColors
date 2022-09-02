class_name PlayerDyingState
extends PlayerBaseState

var light_mask: int
var is_explosion = true

func _init(dependencies: PlayerDependencies).(dependencies):
  light_mask = light_occluder.light_mask
func enter():
  if (is_explosion):
    Event.emit_signal("player_explode")
    light_occluder.light_mask = 0
    animated_sprite.play("die")
    yield(animated_sprite, "animation_finished")
    light_occluder.light_mask = light_mask
    Event.emit_signal("player_died")
  else:
    Event.emit_signal("player_fall")
    player.fallTimerNode.start()
    yield(player.fallTimerNode, "timeout")
    Event.emit_signal("player_died")

func physics_update(delta: float) -> BaseState:
  return .physics_update(delta)

func _on_player_diying(_area, _position, _entity_type) -> BaseState:
    return null

func exit():
  pass
func _physics_update(_delta: float) -> BaseState:
  return null
