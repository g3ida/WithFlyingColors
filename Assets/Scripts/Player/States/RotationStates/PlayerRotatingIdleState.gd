class_name PlayerRotatingIdleState
extends PlayerBaseState

var player_rotation: PlayerRotationAction

func _init(dependencies: PlayerDependencies).(dependencies):
  player_rotation = dependencies.player_rotation_action
  self.base_state = PlayerStatesEnum.IDLE

func enter():
  pass
func exit():
  pass

func physics_update(delta: float) -> BaseState:
  self.player_rotation.step(delta)
  return _handle_rotate()
