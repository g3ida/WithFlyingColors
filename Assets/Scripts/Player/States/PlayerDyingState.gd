class_name PlayerDyingState
extends PlayerBaseState

var light_mask: int

func _init(dependencies: PlayerDependencies).(dependencies):
	light_mask = light_occluder.light_mask
func enter():
	light_occluder.light_mask = 0
	animated_sprite.play("die")
	var _trash = animated_sprite.connect("animation_finished", self, "on_animation_finished", [], CONNECT_ONESHOT)

func on_animation_finished():
	light_occluder.light_mask = light_mask
	Event.emit_signal("player_died")

func physics_update(delta: float) -> BaseState:
	return .physics_update(delta)

func exit():
	pass
func _physics_update(_delta: float) -> BaseState:
	return null
