class_name PlayerRotatingState
extends PlayerBaseState

var rotation_direction: int
var player_rotation: PlayerRotationAction

func _init(dependencies: PlayerDependencies, direction: int).(dependencies):
  player_rotation = dependencies.player_rotation_action
  rotation_direction = direction

func enter():
  self.player_rotation.execute(rotation_direction)
  Event.emit_signal("player_rotate", rotation_direction)
  self.player.scale_face_separators_by(10)
  
func exit():
  self.player.scale_face_separators_by(1)

func physics_update(delta: float) -> BaseState:
  self.player_rotation.step(delta)
  if self.player_rotation.canRotate:
    return self.states_store.get_state(PlayerStatesEnum.IDLE)
  return null

func on_animation_finished(_anim_name) -> BaseState:
	return null