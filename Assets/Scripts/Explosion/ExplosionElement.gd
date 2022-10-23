extends RigidBody2D

var should_detonate = false
var impulse = 0.0

func _ready():
  pass
  
func setup_sprite(texture: Texture, vframes: int, hframes: int, current_frame: int):
  $Sprite.texture = texture
  $Sprite.vframes = vframes
  $Sprite.hframes = hframes
  $Sprite.frame = current_frame

func set_collider_shape(shape: RectangleShape2D):
  $CollisionShape2D.shape = shape

func get_collider():
  return $CollisionShape2D

func detonate(_impulse):
  impulse = _impulse
  should_detonate = true

func _integrate_forces(_state):
  if should_detonate:
    apply_central_impulse(Vector2(rand_range(-impulse, impulse), -impulse))
    should_detonate = false
