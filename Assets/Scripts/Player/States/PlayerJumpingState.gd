class_name PlayerJumpingState
extends PlayerBaseState

const JUMP_FORCE = 1000
var entred = false


func _init(dependencies: PlayerDependencies).(dependencies):
	pass
func enter():
	entred = true
func exit():
	entred = false

func physics_update(delta: float) -> BaseState:
	return .physics_update(delta)

func _physics_update(_delta: float) -> BaseState:
	if (entred):
		entred = false
		player.velocity.y -= JUMP_FORCE
	elif (player.is_on_floor()):
		return self.states_store.get_state(PlayerStatesEnum.STANDING)
	return null