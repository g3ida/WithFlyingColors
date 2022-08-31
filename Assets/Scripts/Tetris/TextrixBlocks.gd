extends KinematicBody2D

export(float) var wait_time = 1.0
export(Vector2) var offset = Vector2(0, 0)

export var units_left := 1
export var units_right := 1
export var units_top := 1
export var units_bottom := 1

signal on_ground_hit()

const DROP_TIME: float = 0.08
const SPEED = Constants.TETRIS_BLOCK_SIZE / DROP_TIME

var next_y_pos: float
var is_waiting := false
var is_dropping := true

func set_global_pos(pos: Vector2):
  self.global_position = pos - offset
  next_y_pos = self.position.y + Constants.TETRIS_BLOCK_SIZE
  
  
func rotate_left():
    self.rot(1)
  
func rotate_right():
  self.rot(-1)
  
func rot(dir: float):
  var angle = 90.0 * PI / 180.0 * dir
  self.rotate(angle)
  self.global_position += offset
  offset = offset.rotated(angle)
  self.global_position -= offset 
  
func _move(delta):
  var dy = SPEED * delta
  var left_dy = next_y_pos - position.y
  if (left_dy < dy):
    dy = left_dy
  var velocity = move_and_slide(Vector2(0.0, dy / delta))
  
  if (abs(position.y - next_y_pos) < 0.01):
    is_waiting = true
    yield(get_tree().create_timer(wait_time), "timeout")
    next_y_pos = self.position.y + Constants.TETRIS_BLOCK_SIZE
    is_waiting = false
  
  if (velocity.y < 0.001):
    is_dropping = false
    emit_signal("on_ground_hit")
  
func _process(delta):
  if (!is_waiting and is_dropping):
    _move(delta)
  
  if (is_dropping):
    if (Input.is_action_just_pressed("rotate_left")):
      rotate_left()
    
    if (Input.is_action_just_pressed("rotate_right")):
      rotate_right()
