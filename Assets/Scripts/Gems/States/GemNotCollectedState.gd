class_name GemNotCollectedState
extends GemBaseState
  
var ocillator: NodeOcillator

const AMPLITUDE: float = 4.0
const ANIMATION_DURATION: float = 4.0
const SHINE_VARIANCE = 0.08
const ROTATION_SPEED = 0.002
  
  
func _init(
  node: Node2D,
  light: Light2D,
  animated_sprite: AnimatedSprite,
  animation_player: AnimationPlayer,
  collision_shape: CollisionPolygon2D,
  shine_sfx: AudioStreamPlayer2D,
  states_store: BaseStatesStore).(node, light, animated_sprite, animation_player, collision_shape, shine_sfx, states_store):
    self.ocillator = NodeOcillator.new(self.node, AMPLITUDE, ANIMATION_DURATION)

func enter():
  self.animation_player.play("RESET")
  self.shine_sfx.play()
    
func physics_update(delta: float) -> BaseState:
  self.light.position = self.animated_sprite.position
  ocillator.update(delta)
  self.light.energy = 1 + SHINE_VARIANCE * sin(2 * PI * ocillator.timer / ANIMATION_DURATION)
  self.light.rotate(ROTATION_SPEED)
  return null
  
func on_collision_with_body(area: Area2D) -> BaseState:
  if (node.group_name in area.get_groups()):
    return self.states_store.collecting
  return null
