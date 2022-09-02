extends Node2D

export var stand_texture: Texture
export var canon_texture: Texture
export var bullet_texture: Texture
export var followNodeName: String 
export (PackedScene) var bullet_scene
export (NodePath) var objectToFollow
export var cooldown := 1.5
export var color_group: String

onready var satndNode = $Stand
onready var canonNode = $Canon
onready var canonMuzzle = $Canon/Muzzle
onready var canonAnimation = $Canon/ShootAnimation
onready var standColorAreaNode = $Body/StandColorArea
onready var canonColorAreaNode = $Body/CanonColorArea
onready var shootSound = $ShoutSound
onready var cooldownTimerNode = $CooldownTimer

var follow: Node = null
var angle: float = 0
var can_shoot: bool = true

const ANGULAR_VELOCITY = 0.5
const VIEW_LIMIT_1 = 179.0 * PI / 180.0
const VIEW_LIMIT_2 = 1.0 * PI / 180.0
const DISTANCE_LIMIT = 6.0 * Global.WORLD_TO_SCREEN
const SHOOT_PRECISION = 2.0 * PI / 180.0 

func _ready():
  follow = get_node(objectToFollow)
  add_to_group(color_group)
  standColorAreaNode.add_to_group(color_group)
  canonColorAreaNode.add_to_group(color_group)
  satndNode.texture = stand_texture
  canonNode.texture = canon_texture
  cooldownTimerNode.wait_time = cooldown
  
func spawn_bullet():
  var bullet = bullet_scene.instance()
  bullet.global_position = canonMuzzle.global_position
  get_parent().add_child(bullet)
  bullet.set_texture(bullet_texture)
  bullet.set_color_group(color_group)
  return bullet
  
func shoot(direction: Vector2):
  can_shoot = false
  canonAnimation.play("Shoot")
  yield(canonAnimation, "animation_finished")
  var bullet = spawn_bullet()
  bullet.shoot(direction)
  shootSound.play()
  cooldownTimerNode.start()
  yield(cooldownTimerNode, "timeout")
  can_shoot = true

func sign_of(x : float) -> float:
  return -1.0 if x < 0 else 1.0

func can_follow(target_angle: float, distance_squared: float) -> bool:
  return not(target_angle > VIEW_LIMIT_1 or target_angle < VIEW_LIMIT_2) and distance_squared < DISTANCE_LIMIT*DISTANCE_LIMIT

func _physics_process(delta):
  var direction: Vector2 = (follow.global_position - canonMuzzle.global_position )
  angle = canonNode.rotation + PI/2.0
  var target_angle = direction.angle()
  var rotation_amount = (target_angle - angle)
  if can_follow(target_angle, direction.length_squared()):
    var amount = ANGULAR_VELOCITY * delta
    if abs(rotation_amount) < abs(amount):
      amount = rotation_amount
    canonNode.rotate(rotation_amount * delta)
  
  if abs(target_angle - angle) < SHOOT_PRECISION and can_shoot:
    shoot(direction.normalized())
