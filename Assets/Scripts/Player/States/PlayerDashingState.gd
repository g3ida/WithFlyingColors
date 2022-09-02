class_name PlayerDashingState
extends PlayerBaseState

const CountdownTimer = preload("res://Assets/Scripts/Utils/CountdownTimer.gd")
const DAH_DURATION = 0.17
const PERMISSIVENESS = 0.05

const DASH_SPEED = 2000

var dash_timer: CountdownTimer
var permissiveness_timer: CountdownTimer
var direction: Vector2
var dash_done = false

func _init(dependencies: PlayerDependencies).(dependencies):
  dash_timer = CountdownTimer.new(DAH_DURATION, false)
  permissiveness_timer = CountdownTimer.new(PERMISSIVENESS, false)
  self.base_state = PlayerStatesEnum.DASHING

func enter():
  direction = Vector2()
  dash_timer.reset()
  permissiveness_timer.reset()
  player.can_dash = false
  dash_done = false
  set_dash_diretion()

func exit():
  if dash_done:
    player.velocity.x = 0
  dash_timer.stop()
  permissiveness_timer.stop()

func physics_update(delta: float) -> BaseState:
  return .physics_update(delta)

func _physics_update(_delta: float) -> BaseState:
  if !dash_done and !permissiveness_timer.is_running():
    set_dash_diretion()
    if direction.length_squared() < 0.01:
      dash_timer.stop()
    else:
      dash_done = true
      Event.emit_signal("player_dash", direction)

  if dash_done:
    if abs(direction.x) > 0.01:
      player.velocity.x = DASH_SPEED * direction.x
    if abs(direction.y) > 0.01:
      player.velocity.y = DASH_SPEED * direction.y

  if !dash_timer.is_running():
    return self.states_store.get_state(PlayerStatesEnum.FALLING)
    
  dash_timer.step(_delta)
  permissiveness_timer.step(_delta)

  return null

func set_dash_diretion():
  if Input.is_action_pressed("move_right") and Input.is_action_pressed("move_left"):
    direction.x = 0
  elif Input.is_action_pressed("move_left"):
    direction.x = -1
  elif Input.is_action_pressed("move_right"):
    direction.x = 1
  elif abs(player.velocity.x) > 0.1:
    direction.x = 1 * Helpers.sign_of(player.velocity.x)
  else:
    direction.x = 0
  if Input.is_action_pressed("down"):
    direction.y = 1
