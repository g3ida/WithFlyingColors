class_name PlayerDashingState
extends PlayerBaseState

const DashGhost = preload("res://Assets/Scenes/Player/DashGhost.tscn")
const CountdownTimer = preload("res://Assets/Scripts/Utils/CountdownTimer.gd")
const DASH_DURATION = 0.17
const PERMISSIVENESS = 0.05

const DASH_SPEED = 20 * Global.WORLD_TO_SCREEN
const DASH_GHOST_INSTANCE_DELAY = 0.04

var dash_timer: CountdownTimer
var permissiveness_timer: CountdownTimer
var direction = null
var dash_done = false

func _init(dependencies: PlayerDependencies).(dependencies):
  dash_timer = CountdownTimer.new(DASH_DURATION, false)
  permissiveness_timer = CountdownTimer.new(PERMISSIVENESS, false)
  self.base_state = PlayerStatesEnum.DASHING
  player.dashGhostTimerNode.wait_time = DASH_GHOST_INSTANCE_DELAY
  var __ = player.dashGhostTimerNode.connect("timeout", self, "_on_dash_ghost_timer_timeout")


func enter():
  if direction == null:
    permissiveness_timer.reset()
    set_dash_diretion()
    dash_done = false
  else: # in case of touch input we already have dash direction
    dash_done = true
    permissiveness_timer.stop()

  dash_timer.reset()
  player.can_dash = false
  Global.camera.get_node("CameraShake").start()  
  instance_ghost()
  player.dashGhostTimerNode.start()

func exit():
  if dash_done:
    player.velocity.x = 0
  dash_timer.stop()
  permissiveness_timer.stop()
  player.dashGhostTimerNode.stop()
  direction = null


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
  else:
    player.velocity.y = 0
    
  dash_timer.step(_delta)
  permissiveness_timer.step(_delta)

  return null

func set_dash_diretion():
  direction = Vector2()
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

func _on_dash_ghost_timer_timeout():
  instance_ghost()

func instance_ghost():
  var ghost: Sprite = DashGhost.instance()
  ghost.scale = player.scale
  player.get_parent().add_child(ghost) #todo: maybe should we set_owner for the ghost ?
  ghost.global_position = player.global_position
  ghost.texture = player.animatedSpriteNode.frames.get_frame(player.animatedSpriteNode.animation, player.animatedSpriteNode.frame)
  ghost.rotate((player.rotation))
