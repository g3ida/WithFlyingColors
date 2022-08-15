class_name GemCollectingState
extends GemBaseState

var is_active = false

func _init(
	node: Node2D,
	light: Light2D,
	animated_sprite: AnimatedSprite,
	animation_player: AnimationPlayer,
	collision_shape: CollisionShape2D,
	states_store: BaseStatesStore).(node, light, animated_sprite, animation_player, collision_shape, states_store):
		pass

func enter():
	self.collision_shape.set_deferred("disabled", true)
	self.animation_player.play("gem_collected_animation")

func exit():
	self.collision_shape.set_deferred("disabled", false)

func on_animation_finished(anim_name) -> BaseState:
	if anim_name == "gem_collected_animation":
		Event.emit_signal(
			"gem_collected",
			self.node.group_name,
			self.animated_sprite.get_global_transform_with_canvas().origin,
			self.animated_sprite.frames)
		return self.states_store.collected
	return null
