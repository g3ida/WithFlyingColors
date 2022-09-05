class_name PlayerRotatingState
extends PlayerBaseState

var rotation_direction: int
var player_rotation: PlayerRotationAction

func _init(dependencies: PlayerDependencies, direction: int).(dependencies):
  player_rotation = dependencies.player_rotation_action
  rotation_direction = direction
  self.base_state = PlayerStatesEnum.ROTATING_LEFT if direction == -1 else PlayerStatesEnum.ROTATING_RIGHT


func enter():
  var cumulate_angle = false if player.player_state.base_state == PlayerStatesEnum.SLIPPERING else true
  self.player_rotation.execute(rotation_direction, Constants.PI2, 0.1, true, cumulate_angle)
  Event.emit_signal("player_rotate", rotation_direction)
  
func exit():
  pass

func physics_update(delta: float) -> BaseState:
  self.player_rotation.step(delta)
  if self.player_rotation.canRotate:
    return self.states_store.get_state(PlayerStatesEnum.IDLE)
  if Input.is_action_just_pressed("rotate_left"):
    return self.states_store.get_state(PlayerStatesEnum.ROTATING_LEFT)
  if Input.is_action_just_pressed("rotate_right"):
    return self.states_store.get_state(PlayerStatesEnum.ROTATING_RIGHT)
  return null

func on_animation_finished(_anim_name) -> BaseState:
  return null
