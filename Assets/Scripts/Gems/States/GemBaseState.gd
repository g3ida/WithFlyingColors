class_name GemBaseState
extends BaseState

var node: Node2D
var light: Light2D
var animated_sprite: AnimatedSprite
var animation_player: AnimationPlayer
var collision_shape: CollisionPolygon2D
var shine_sfx: AudioStreamPlayer2D
var states_store: BaseStatesStore
  
func _init(
  _node: Node2D,
  _light: Light2D,
  _animated_sprite: AnimatedSprite,
  _animation_player: AnimationPlayer,
  _collision_shape: CollisionPolygon2D,
  _shine_sfx: AudioStreamPlayer2D,
  _states_store: BaseStatesStore).():
    self.node = _node
    self.light = _light
    self.animated_sprite = _animated_sprite
    self.animation_player = _animation_player
    self.collision_shape = _collision_shape
    self.shine_sfx = _shine_sfx
    self.states_store = _states_store
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