class_name PlayerFallingState
extends PlayerBaseState

func _init(dependencies: PlayerDependencies).(dependencies):
	pass
func enter():
	animated_sprite.play("idle")
	animated_sprite.playing = false
func exit():
	pass

func physics_update(delta: float) -> BaseState:
	return .physics_update(delta)

func _physics_update(_delta: float) -> BaseState:
	if (player.is_on_floor()):
		return self.states_store.get_state(PlayerStatesEnum.STANDING)
	return null
func on_animation_finished(_anim_name) -> BaseState:
	return null