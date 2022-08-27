class_name PlayerBaseState
extends BaseState

var player: KinematicBody2D
var light_occluder: LightOccluder2D
var animated_sprite: AnimatedSprite
var collision_shape: CollisionShape2D
var states_store: BaseStatesStore


const TransoformAnimation = preload("res://Assets/Scripts/Utils/TransformAnimation.gd")
const ElasticOut = preload("res://Assets/Scripts/Utils/Interpolation/ElasticOut.gd")

const SPEED = 350
const GRAVITY = 980
const FALL_FACTOR = 2

func _init(dependencies: PlayerDependencies).():
		self.player = dependencies.player
		self.light_occluder = dependencies.light_occluder
		self.collision_shape = dependencies.collision_shape
		self.animated_sprite = dependencies.animated_sprite
		self.states_store = dependencies.states_store

func enter():
	pass
func exit():
	pass
func physics_update(delta: float) -> BaseState:
	if Input.is_action_pressed("move_right"):
		player.velocity.x = SPEED
	elif Input.is_action_pressed("move_left"):
		player.velocity.x = -SPEED
	
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

func _on_player_diying(_area, _position) -> BaseState:
	var dying_state = self.states_store.get_state(PlayerStatesEnum.DYING)
	dying_state.is_explosion = false if _area == null else true #if fallzone or platform
	return dying_state

func on_land() -> BaseState:
	player.current_animation = player.scale_animation
	if not player.current_animation.isRunning():
		player.current_animation.start()
	return null