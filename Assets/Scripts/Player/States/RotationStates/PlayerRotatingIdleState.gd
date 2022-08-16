class_name PlayerRotatingIdleState
extends PlayerBaseState

var player_rotation: PlayerRotationAction

func _init(dependencies: PlayerDependencies).(dependencies):
  player_rotation = dependencies.player_rotation_action
func enter():
  pass
func exit():
	pass

func physics_update(delta: float) -> BaseState:
  self.player_rotation.step(delta)
  if Input.is_action_just_pressed("rotate_left"):
    return self.states_store.get_state(PlayerStatesEnum.ROTATING_LEFT)
  if Input.is_action_just_pressed("rotate_right"):
    return self.states_store.get_state(PlayerStatesEnum.ROTATING_RIGHT)
  return null