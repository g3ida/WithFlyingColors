extends KinematicBody2D

const PlayerRotationAction = preload("res://Assets/Scripts/PlayerRotationAction.gd")
const SPEED = 250
const GRAVITY = 980
const JUMP_FORCE = 1000
const FALL_FACTOR = 2

var velocity = Vector2(0, 0)
var playerRotationAction: PlayerRotationAction

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
	velocity = move_and_slide(velocity, Vector2.UP)
	velocity.x = lerp(velocity.x, 0, 0.25)

func connect_signals():
	Event.connect("player_diying", self, "_on_player_diying")

func disconnect_signals():
	Event.disconnect("player_diying", self, "_on_player_diying")
	
func _enter_tree():
	connect_signals()

func _exit_tree():
	disconnect_signals()
	
func _on_player_diying(area, position):
	$AnimatedSprite.play("die")
	$AnimatedSprite.connect("animation_finished", self, "_on_player_died", [], CONNECT_ONESHOT)

func _on_player_died():
	Event.emit_signal("player_died")

func _ready():
	playerRotationAction = PlayerRotationAction.new(self)
