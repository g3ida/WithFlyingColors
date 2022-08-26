class_name GemCollectedState
extends GemBaseState


func _init(
  node: Node2D,
  light: Light2D,
  animated_sprite: AnimatedSprite,
  animation_player: AnimationPlayer,
  collision_shape: CollisionPolygon2D,
  states_store: BaseStatesStore).(node, light, animated_sprite, animation_player, collision_shape, states_store):
    pass

func enter():
  self.collision_shape.set_deferred("disabled", true)
  self.node.set_deferred("visible", false)
  
func exit():
  self.collision_shape.set_deferred("disabled", false)
  self.node.set_deferred("visible", true)
