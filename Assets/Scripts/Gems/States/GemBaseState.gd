class_name GemBaseState
extends BaseState

var node: Node2D
var light: Light2D
var animated_sprite: AnimatedSprite
var animation_player: AnimationPlayer
var collision_shape: CollisionPolygon2D
var states_store: BaseStatesStore
  
func _init(
  node: Node2D,
  light: Light2D,
  animated_sprite: AnimatedSprite,
  animation_player: AnimationPlayer,
  collision_shape: CollisionPolygon2D,
  states_store: BaseStatesStore).():
    self.node = node
    self.light = light
    self.animated_sprite = animated_sprite
    self.animation_player = animation_player
    self.collision_shape = collision_shape
    self.states_store = states_store
func enter():
  pass
func exit():
  pass
func physics_update(_delta: float) -> BaseState:
  return null
func on_collision_with_body(_body) -> BaseState:
  return null
func on_animation_finished(_anim_name) -> BaseState:
  return null
