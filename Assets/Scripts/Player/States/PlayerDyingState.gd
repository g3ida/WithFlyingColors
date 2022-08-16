class_name PlayerDyingState
extends PlayerBaseState

func _init(dependencies: PlayerDependencies).(dependencies):
    pass
func enter():
	animated_sprite.play("die")
	var _trash = animated_sprite.connect("animation_finished", self, "on_animation_finished", [], CONNECT_ONESHOT)

func on_animation_finished():
	Event.emit_signal("player_died")

func physics_update(delta: float) -> BaseState:
	return .physics_update(delta)

func exit():
	pass
func _physics_update(_delta: float) -> BaseState:
	return null