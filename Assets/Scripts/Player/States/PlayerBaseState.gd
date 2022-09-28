class_name PlayerBaseState
extends BaseState

var player: KinematicBody2D
var light_occluder: LightOccluder2D
var animated_sprite: AnimatedSprite
var collision_shape: CollisionShape2D
var jump_particles: CPUParticles2D
var states_store: BaseStatesStore
var base_state
var player_moved = false

const TransoformAnimation = preload("res://Assets/Scripts/Utils/TransformAnimation.gd")
const ElasticOut = preload("res://Assets/Scripts/Utils/Interpolation/ElasticOut.gd")

const GRAVITY = 9.8 * Global.WORLD_TO_SCREEN
const FALL_FACTOR = 2.5

func _init(dependencies: PlayerDependencies).():
    self.player = dependencies.player
    self.light_occluder = dependencies.light_occluder
    self.collision_shape = dependencies.collision_shape
    self.animated_sprite = dependencies.animated_sprite
    self.jump_particles = dependencies.player_jump_particles
    self.states_store = dependencies.states_store

func _enter():
  reset_touch_input()
  player.scale_corners_by(player.current_default_corner_scale_factor)
  enter()

func _exit():
  reset_touch_input()
  exit()

func enter():
  pass
func exit():
  pass

func input(_event):
  pass

func _input(event):
  if event is InputTouchMove:
    player.touch_move_input = event
  elif event is InputTouchJump:
    player.touch_jump_input = event
  elif event is InputTouchDash:
    player.touch_dash_input = event
  elif event is InputTouchRotate:
    player.touch_rotation_input = event
  input(event)

func physics_update(delta: float) -> BaseState:
  player_moved = false
  if player.player_state != self.states_store.get_state(PlayerStatesEnum.DYING):
    if (Input.is_action_just_pressed("dash") or player.touch_dash_input) and player.can_dash:
      return _on_dash()

    if player.player_state != self.states_store.get_state(PlayerStatesEnum.DASHING):
      if Input.is_action_pressed("move_right"):
        player_moved = true
        player.velocity.x = clamp(player.velocity.x + player.speed_unit, 0, player.speed_limit)
      elif Input.is_action_pressed("move_left"):
        player_moved = true
        player.velocity.x = clamp(player.velocity.x - player.speed_unit, -player.speed_limit, -0)
      
      elif (player.touch_move_input != null):
        player_moved = true
        var min_v = min(sign(player.touch_move_input.direction.x) * player.speed_limit, 0)
        var max_v = max(sign(player.touch_move_input.direction.x) * player.speed_limit, 0)
        player.velocity.x = clamp(player.velocity.x + player.touch_move_input.direction.x * player.speed_unit, min_v, max_v)
    
    player.velocity.y += GRAVITY * delta * FALL_FACTOR

  var new_state = _physics_update(delta)
  
  player.velocity = player.move_and_slide(player.velocity, Vector2.UP)
  player.velocity.x = lerp(player.velocity.x, 0, 0.25)
  player.current_animation.step(player, animated_sprite, delta)

  if (new_state):
    return new_state
  else:
    reset_touch_input()
    return null

func reset_touch_input():
  player.touch_jump_input = null
  player.touch_dash_input = null
  player.touch_rotation_input = null

func _physics_update(_delta) -> BaseState:
  return null

func _on_player_diying(_area, _position, entity_type) -> BaseState:
  var dying_state = self.states_store.get_state(PlayerStatesEnum.DYING)
  dying_state.is_explosion = false if entity_type == Global.EntityType.FALLZONE else true
  return dying_state

func on_land() -> BaseState:
  player.current_animation = player.scale_animation
  if not player.current_animation.isRunning():
    player.current_animation.start()
  return null

func _on_dash() -> BaseState:
  var dashing_state = self.states_store.get_state(PlayerStatesEnum.DASHING)
  if player.touch_dash_input != null:
    dashing_state.direction = player.touch_dash_input.direction
  return dashing_state

func _on_jump() -> BaseState:
  var jump_state = self.states_store.get_state(PlayerStatesEnum.JUMPING)
  if (player.touch_jump_input != null):
    jump_state.touch_jump_power = player.touch_jump_input.force
  return jump_state

func _jump_pressed() -> bool:
  return Input.is_action_just_pressed("jump") or player.touch_jump_input != null

func _handle_rotate():
  if Input.is_action_just_pressed("rotate_left"):
    return self.states_store.get_state(PlayerStatesEnum.ROTATING_LEFT)
  if Input.is_action_just_pressed("rotate_right"):
    return self.states_store.get_state(PlayerStatesEnum.ROTATING_RIGHT)
  #touch
  if player.touch_rotation_input != null:
    if player.touch_rotation_input.direction > 0:
      return self.states_store.get_state(PlayerStatesEnum.ROTATING_RIGHT)
    else:
      return self.states_store.get_state(PlayerStatesEnum.ROTATING_LEFT)
  return null
