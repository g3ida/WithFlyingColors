class_name PlayerBaseState
extends BaseState

var player: KinematicBody2D
var light_occluder: LightOccluder2D
var animated_sprite: AnimatedSprite
var collision_shape: CollisionShape2D
var jump_particles: CPUParticles2D
var states_store: BaseStatesStore
var base_state

const TransoformAnimation = preload("res://Assets/Scripts/Utils/TransformAnimation.gd")
const ElasticOut = preload("res://Assets/Scripts/Utils/Interpolation/ElasticOut.gd")

const SPEED_UNIT = 70
const SPEED = 350
const GRAVITY = 980
const FALL_FACTOR = 2.5

func _init(dependencies: PlayerDependencies).():
    self.player = dependencies.player
    self.light_occluder = dependencies.light_occluder
    self.collision_shape = dependencies.collision_shape
    self.animated_sprite = dependencies.animated_sprite
    self.jump_particles = dependencies.player_jump_particles
    self.states_store = dependencies.states_store

func enter():
  pass
func exit():
  pass
func physics_update(delta: float) -> BaseState:
  if player.player_state != self.states_store.get_state(PlayerStatesEnum.DYING):
    if Input.is_action_just_pressed("dash") and player.can_dash:
      return self.states_store.get_state(PlayerStatesEnum.DASHING)

    if player.player_state != self.states_store.get_state(PlayerStatesEnum.DASHING):
      if Input.is_action_pressed("move_right"):
        player.velocity.x = clamp(player.velocity.x + SPEED_UNIT, 0, SPEED)
      elif Input.is_action_pressed("move_left"):
        player.velocity.x = clamp(player.velocity.x - SPEED_UNIT, -SPEED, -0)

    
    player.velocity.y += GRAVITY * delta * FALL_FACTOR

  var new_state = _physics_update(delta)
  
  player.velocity = player.move_and_slide(player.velocity, Vector2.UP)
  player.velocity.x = lerp(player.velocity.x, 0, 0.25)

  player.current_animation.step(player, animated_sprite, delta)

  if (new_state):
    return new_state
  else:
    return null

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
