class_name PlayerFallingState
extends PlayerBaseState

const CountdownTimer = preload("res://Assets/Scripts/Utils/CountdownTimer.gd")
var permissiveness_timer: CountdownTimer
const PERMISSIVENESS = 0.04

func _init(dependencies: PlayerDependencies).(dependencies):
	permissiveness_timer = CountdownTimer.new(PERMISSIVENESS, false)
func enter():
	animated_sprite.play("idle")
	animated_sprite.playing = false
	permissiveness_timer.reset()
func exit():
	permissiveness_timer.stop()

func physics_update(delta: float) -> BaseState:
	return .physics_update(delta)

func _physics_update(_delta: float) -> BaseState:
	if (player.is_on_floor()):
		return self.states_store.get_state(PlayerStatesEnum.STANDING)
	if Input.is_action_just_pressed("jump") and permissiveness_timer.is_running():
		permissiveness_timer.stop()
		return self.states_store.get_state(PlayerStatesEnum.JUMPING)
	permissiveness_timer.step(_delta)
	return null
func on_animation_finished(_anim_name) -> BaseState:
	return null