class_name GemStatesStore
extends BaseStatesStore

const StatesEnum = preload("res://Assets/Scripts/Gems/States/GemStatesEnum.gd")

var not_collected: GemBaseState
var collecting: GemBaseState
var collected: GemBaseState

func _init(node: Node2D,
  light: Light2D,
  animated_sprite: AnimatedSprite,
  animation_player: AnimationPlayer,
  collision_shape: CollisionPolygon2D,
  shine_sfx: AudioStreamPlayer2D):
    not_collected = GemNotCollectedState.new(node, light, animated_sprite, animation_player, collision_shape, shine_sfx, self as BaseStatesStore)
    collecting = GemCollectingState.new(node, light, animated_sprite, animation_player, collision_shape, shine_sfx, self)
    collected = GemCollectedState.new(node, light, animated_sprite, animation_player, collision_shape, shine_sfx, self)

func get_state(state: int):
  if state == StatesEnum.NOT_COLLECTED:
    return not_collected
  if state == StatesEnum.COLLECTING:
    return collecting
  if state == StatesEnum.COLLECTED:
    return collected
  return null
