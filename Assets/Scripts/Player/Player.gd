extends KinematicBody2D

const PlayerRotationAction = preload("res://Assets/Scripts/PlayerRotationAction.gd")
const TransoformAnimation = preload("res://Assets/Scripts/Utils/TransformAnimation.gd")
const ElasticIn = preload("res://Assets/Scripts/Utils/Interpolation/ElasticIn.gd")
const ElasticOut = preload("res://Assets/Scripts/Utils/Interpolation/ElasticOut.gd")

const SPEED = 250
const GRAVITY = 980
const JUMP_FORCE = 1000
const FALL_FACTOR = 2

const SQUEEZE_ANIM_DURATION = 0.17
const SCALE_ANIM_DURATION = 0.17

var velocity = Vector2(0, 0)
var playerRotationAction: PlayerRotationAction

var scale_animation: TransoformAnimation
var idle_animation: TransoformAnimation
var current_animation: TransoformAnimation

var sprite_size: int
var was_on_floor: bool = true

var reset_position: Vector2
var reset_angle: float = 0

func _ready():
	playerRotationAction = PlayerRotationAction.new(self)
	sprite_size = $AnimatedSprite.frames.get_frame("idle", 0).get_width()
	
	idle_animation = TransoformAnimation.new(
		0.0,
		ElasticOut.new(1.0, 1.0, 1, 0.1),
		0)
	scale_animation = TransoformAnimation.new(
		SCALE_ANIM_DURATION,
		ElasticOut.new(1.0, 1.0, 1, 0.1),
		sprite_size * 0.5)
		
	current_animation = idle_animation
	was_on_floor = is_on_floor()
	self.reset_position = self.global_position

func _physics_process(delta):
	if Input.is_action_pressed("move_right"):
		velocity.x = SPEED
	elif Input.is_action_pressed("move_left"):
		velocity.x = -SPEED
	
	velocity.y += GRAVITY * delta * FALL_FACTOR

	if Input.is_action_just_pressed("jump") and is_on_floor():
		velocity.y -= JUMP_FORCE
		
	if Input.is_action_just_pressed("rotate_left"):
		playerRotationAction.execute(-1)
	if Input.is_action_just_pressed("rotate_right"):
		playerRotationAction.execute(1)	
		
	playerRotationAction.step(delta)
	current_animation.step(self, $AnimatedSprite, delta)
	
	velocity = move_and_slide(velocity, Vector2.UP)
	velocity.x = lerp(velocity.x, 0, 0.25)
	
	var on_floor = is_on_floor()
	if (not was_on_floor) and on_floor:
		on_land()
	was_on_floor = on_floor

func reset():
	$AnimatedSprite.play("idle")
	$AnimatedSprite.playing = false
	self.global_position = reset_position
	self.rotate(self.reset_angle - self.rotation)

func connect_signals():
	Event.connect("player_diying", self, "_on_player_diying")
	Event.connect("checkpoint", self, "_on_checkpoint_hit")
	
func disconnect_signals():
	Event.disconnect("player_diying", self, "_on_player_diying")
	Event.disconnect("checkpoint", self, "_on_checkpoint_hit")
		
func _enter_tree():
	Global.player = self
	connect_signals()

func _exit_tree():
	disconnect_signals()
	
func _on_player_diying(area, position):
	$AnimatedSprite.play("die")
	$AnimatedSprite.connect("animation_finished", self, "_on_player_died", [], CONNECT_ONESHOT)

func _on_player_died():
	Event.emit_signal("player_died")
	
func on_land():
	current_animation = scale_animation
	if not current_animation.isRunning():
		current_animation.start()

func _on_checkpoint_hit(checkpoint_object: Node2D):
	self.reset_position = checkpoint_object.global_position
	
	if checkpoint_object.color_group in $BottomFace.get_groups():
		self.reset_angle = 0
	elif checkpoint_object.color_group in $LeftFace.get_groups():
		self.reset_angle = -PI / 2
	elif checkpoint_object.color_group in $RightFace.get_groups():
		self.reset_angle = PI / 2
	elif checkpoint_object.color_group in $TopFace.get_groups():
		self.reset_angle = PI
