class_name PlayerStandingState
extends PlayerBaseState

func _init(dependencies: PlayerDependencies).(dependencies):
	pass

func enter():
	self.animated_sprite.play("idle")
	animated_sprite.playing = false
	Event.emit_signal("player_land")
func exit():
	pass

func physics_update(delta: float) -> BaseState:
	return .physics_update(delta)

func _physics_update(_delta: float) -> BaseState:
	if Input.is_action_just_pressed("jump") and player.is_on_floor():
		return self.states_store.get_state(PlayerStatesEnum.JUMPING)
	if not player.is_on_floor():
		return self.states_store.get_state(PlayerStatesEnum.FALLING)
	return null