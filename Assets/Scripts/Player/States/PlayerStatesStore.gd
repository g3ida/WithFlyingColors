class_name PlayerStatesStore
extends BaseStatesStore

#player states
var standing_state: PlayerStandingState
var jumping_state: PlayerJumpingState
var falling_state: PlayerFallingState
var dying_state: PlayerDyingState
var dashing_state: PlayerDashingState

#rotation states
var rotating_right_state: PlayerRotatingState
var rotating_left_state: PlayerRotatingState
var idle_state: PlayerRotatingIdleState

func _init(player):
  var dependencies = PlayerDependencies.new(player)
  dependencies.states_store = self

  standing_state = PlayerStandingState.new(dependencies)
  jumping_state = PlayerJumpingState.new(dependencies)
  falling_state = PlayerFallingState.new(dependencies)
  dying_state = PlayerDyingState.new(dependencies)
  dashing_state = PlayerDashingState.new(dependencies)

  rotating_right_state = PlayerRotatingState.new(dependencies, 1)
  rotating_left_state = PlayerRotatingState.new(dependencies, -1)
  idle_state = PlayerRotatingIdleState.new(dependencies)

func get_state(_state: int):
  if _state ==  PlayerStatesEnum.IDLE:
    return idle_state
  if _state ==  PlayerStatesEnum.ROTATING_LEFT:
    return rotating_left_state
  if _state ==  PlayerStatesEnum.ROTATING_RIGHT:
    return rotating_right_state
  if _state ==  PlayerStatesEnum.STANDING:
    return standing_state
  if _state ==  PlayerStatesEnum.JUMPING:
    return jumping_state
  if _state ==  PlayerStatesEnum.FALLING:
    return falling_state
  if _state ==  PlayerStatesEnum.DYING:
    return dying_state
  if _state ==  PlayerStatesEnum.DASHING:
    return dashing_state
  return null
