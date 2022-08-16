class_name PlayerJumpingState
extends PlayerBaseState

const CountdownTimer = preload("res://Assets/Scripts/Utils/CountdownTimer.gd")
const TIME_UNTIL_FULL_JUMP_IS_CONSIDERED = 0.15
const PERMISSIVENESS = 0.09

const JUMP_FORCE = 1000
var entred = false
var jump_timer: CountdownTimer
var permissiveness_timer: CountdownTimer

func _init(dependencies: PlayerDependencies).(dependencies):
	jump_timer = CountdownTimer.new(TIME_UNTIL_FULL_JUMP_IS_CONSIDERED, false)
	permissiveness_timer = CountdownTimer.new(PERMISSIVENESS, false)

func enter():
	entred = true
	jump_timer.reset()

func exit():
	entred = false
	jump_timer.stop()
	permissiveness_timer.stop()

func physics_update(delta: float) -> BaseState:
	return .physics_update(delta)

func _physics_update(_delta: float) -> BaseState:
	if (entred):
		entred = false
		player.velocity.y -= JUMP_FORCE
	elif (player.is_on_floor()):
		if permissiveness_timer.is_running():
			return self.states_store.get_state(PlayerStatesEnum.JUMPING)
		else:
			return self.states_store.get_state(PlayerStatesEnum.STANDING)

	if (Input.is_action_just_pressed("jump")):
		permissiveness_timer.reset()

	if jump_timer.is_running() and Input.is_action_just_released("jump"):
		jump_timer.stop()
		if (player.velocity.y < 0):
			player.velocity.y *= 0.5

	jump_timer.step(_delta)
	permissiveness_timer.step(_delta)

	return null