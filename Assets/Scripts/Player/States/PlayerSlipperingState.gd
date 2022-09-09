class_name PlayerSlipperingState
extends PlayerBaseState

var direction := 1
var player_rotation: PlayerRotationAction

const CORRECT_ROTATION_FALL_SPEED = 0.3
const CORRECT_ROTATION_JUMP_SPEED = 0.07
const PLAYER_SPEED_THRESHOLD_TO_STAND = -100.0
const SLIPPERING_ROTATION_DURATION = 3.0
const SLIPPERING_RECOVERY_INITIAL_DURATION = 0.8

var exit_rotation_speed = CORRECT_ROTATION_JUMP_SPEED
var skip_exit_rotation = false

func _init(dependencies: PlayerDependencies).(dependencies):
  self.base_state = PlayerStatesEnum.SLIPPERING
  player_rotation = dependencies.player_rotation_action

func enter():
  self.animated_sprite.play("idle")
  animated_sprite.playing = false
  skip_exit_rotation = false
  exit_rotation_speed = CORRECT_ROTATION_JUMP_SPEED
  self.player_rotation.execute(direction, Constants.PI2, SLIPPERING_ROTATION_DURATION, true, false, true)
  Event.emit_signal("player_slippering")
  player.can_dash = true

func exit():
  # the fact that I am splitting this into a slow then rapid action is for these reasons:
  # 1- to prevent collision if the player jumped (if rotation speed is high move_and_slide
  #    won't work because the player will touch the platform before jump is completed)
  # 2- to make falling less sudden (rotation should be slow for visual appeal and fast
  #    for gameplay so the combination is the best option )
  if not skip_exit_rotation:
    self.player_rotation.execute(-direction, Constants.PI2, SLIPPERING_RECOVERY_INITIAL_DURATION, true, false, false)
    yield(player.get_tree().create_timer(0.05), "timeout")
    self.player_rotation.execute(-direction, Constants.PI2, exit_rotation_speed, true, false, false)
  
func physics_update(delta: float) -> BaseState:
  return .physics_update(delta)

func _physics_update(_delta: float) -> BaseState:
  if _jump_pressed() and player.is_on_floor(): 
    exit_rotation_speed = CORRECT_ROTATION_JUMP_SPEED
    return _on_jump()

  if not player.is_on_floor():
    var falling_state = self.states_store.get_state(PlayerStatesEnum.FALLING)
    exit_rotation_speed = CORRECT_ROTATION_FALL_SPEED
    falling_state.was_on_floor = true
    direction = -direction
    return falling_state
  
  if (player.player_rotation_state.base_state != PlayerStatesEnum.IDLE):
    skip_exit_rotation = true
    return self.states_store.get_state(PlayerStatesEnum.STANDING)

  if self.player_rotation.canRotate or self.player.velocity.x*direction < PLAYER_SPEED_THRESHOLD_TO_STAND:
    return self.states_store.get_state(PlayerStatesEnum.STANDING)
  return null
