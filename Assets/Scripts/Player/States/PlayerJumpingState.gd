class_name PlayerJumpingState
extends PlayerBaseState

const CountdownTimer = preload("res://Assets/Scripts/Utils/CountdownTimer.gd")
const TIME_UNTIL_FULL_JUMP_IS_CONSIDERED = 0.15
const PERMISSIVENESS = 0.09
const FACE_SEPARATOR_SCALE_FACTOR = 4.5

const JUMP_FORCE = 1200
var entred = false
var jump_timer: CountdownTimer
var permissiveness_timer: CountdownTimer
var touch_jump_power: float = 1.0

func _init(dependencies: PlayerDependencies).(dependencies):
  jump_timer = CountdownTimer.new(TIME_UNTIL_FULL_JUMP_IS_CONSIDERED, false)
  permissiveness_timer = CountdownTimer.new(PERMISSIVENESS, false)
  self.base_state = PlayerStatesEnum.JUMPING

func enter():
  entred = true
  jump_timer.reset()
  Event.emit_signal("player_jumped")
  jump_particles.emitting = true
  self.player.scale_corners_by(FACE_SEPARATOR_SCALE_FACTOR)

func exit():
  entred = false
  jump_timer.stop()
  permissiveness_timer.stop()
  jump_particles.emitting = false
  self.player.scale_corners_by(1)
  touch_jump_power = 1.0

func physics_update(delta: float) -> BaseState:
  return .physics_update(delta)

func _physics_update(_delta: float) -> BaseState:
  if (entred):
    entred = false
    player.velocity.y -= JUMP_FORCE * touch_jump_power
  elif player.is_on_floor():
    if permissiveness_timer.is_running():
      return self.states_store.get_state(PlayerStatesEnum.JUMPING)
    else:
      return self.states_store.get_state(PlayerStatesEnum.STANDING)

  if _jump_pressed():
    permissiveness_timer.reset()

  #todo: handle this in touch
  if jump_timer.is_running() and Input.is_action_just_released("jump"):
    jump_timer.stop()
    if (player.velocity.y < 0):
      player.velocity.y *= 0.5

  jump_timer.step(_delta)
  permissiveness_timer.step(_delta)
  return null

func with_jump_power(jump_power: float):
  touch_jump_power = jump_power
  return self