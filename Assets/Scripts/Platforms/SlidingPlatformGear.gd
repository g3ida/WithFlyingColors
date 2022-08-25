extends Sprite

var last_position: Vector2
const rotation_speed = 0.01

func _ready():
  #set parent scale so the sprite won't be affected.
  var parent_platform_scale = get_parent().get_parent().scale
  get_parent().scale = Vector2(1/parent_platform_scale.x, 1/parent_platform_scale.y)
  last_position = global_position

func _physics_process(_delta):
  var current_positon = global_position
  var delta_position = current_positon - last_position
  last_position = current_positon
  
  var direction = 0
  if delta_position.x > Global.EPSILON or delta_position.y > Global.EPSILON:
    direction = 1
  elif delta_position.x < -Global.EPSILON or delta_position.y < -Global.EPSILON:
    direction = -1
  
  rotate(rotation_speed * delta_position.length() * direction)
