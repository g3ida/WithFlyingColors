class_name Tetromino
extends Node2D

const DIRECTIONS := 4

var rotation_map: Array
var rotate_index: int = 0

var grid = []

func inc_rotate_index() -> void: rotate_index = (rotate_index + 1) % DIRECTIONS
func dec_rotate_index() -> void: rotate_index = (rotate_index - 1) % DIRECTIONS
func move_rotate_index_by(dir: int) -> void: rotate_index = abs((rotate_index + dir) % DIRECTIONS)

func set_grid(_grid: Array):
  grid = _grid
  for ch in get_children():
    ch.grid = _grid

func _ready():
  pass

func move_down() -> void: move_by(0, 1)
func move_down_safe() -> bool: return move_by_safe(0, 1)


func move_left() -> void: move_by(-1, 0)
func move_left_safe() -> bool: return move_by_safe(-1, 0)

func move_right() -> void: move_by(1, 0)
func move_right_safe() -> bool: return move_by_safe(1, 0)

func rotate_left() -> void: rotate_dir(-1)
func rotate_left_safe() -> bool: return rotate_dir_safe(-1)

func rotate_right() -> void: rotate_dir(-1)
func rotate_right_safe() -> bool: return rotate_dir_safe(1)


func rotate_dir(dir: int):
  var old_idx = rotate_index
  move_rotate_index_by(dir)
  var i = 0
  for ch in get_children():
    var pos = rotation_map[rotate_index][i]
    var old_pos = rotation_map[old_idx][i]
    var d_dist = pos - old_pos
    ch.move_by(int(d_dist.x), int(d_dist.y))
    i += 1
  set_shape()

func rotate_dir_safe(dir: int) -> bool:
  if can_rotate_dir(dir):
    rotate_dir(dir)
    return true
  return false

func can_move_down(): return can_move_by(0, 1)
func can_move_left(): return can_move_by(-1, 0)
func can_move_right(): return can_move_by(1, 0)

func can_rotate_dir(dir: int):
  var old_idx = rotate_index
  move_rotate_index_by(dir)
  var i = 0
  for ch in get_children():
    var pos = rotation_map[rotate_index][i]
    var old_pos = rotation_map[old_idx][i]
    var d_dist = pos - old_pos
    if not ch.can_move_by(int(d_dist.x), int(d_dist.y)):
      move_rotate_index_by(-dir)
      return false
    i += 1
  move_rotate_index_by(-dir)
  return true

func can_move_by(i: int, j: int) -> bool:
  for ch in get_children():
    if not ch.can_move_by(i, j):
      return false
  return true

func move_by(i: int, j: int) -> void:
  for ch in get_children():
    ch.move_by(i, j)
  self.position.x += i * Constants.TETRIS_BLOCK_SIZE
  self.position.y += j * Constants.TETRIS_BLOCK_SIZE

func move_by_safe(i: int, j: int) -> bool:
  if can_move_by(i, j):
    move_by(i, j)
    return true
  return false

func add_to_grid():
  for ch in get_children():
    ch.add_to_grid()

func remove_from_grid():
  for ch in get_children():
    ch.remove_from_grid()

func set_shape():
  var i=0
  for ch in get_children():
    ch.position = rotation_map[rotate_index][i] * Constants.TETRIS_BLOCK_SIZE
    i += 1
