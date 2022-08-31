extends Node2D

export var texture: Texture
export var color_group: String
export var i: int = 0
export var j: int = 0

signal block_destroyed

onready var spriteNode = $Sprite
onready var spriteAnimationNode = $Sprite/AnimationPlayer
onready var areaNode = $Area2D

var blink_animation_duration = 0.5
var is_active = true
var grid = []

func move_down() -> void:
  j += 1

func move_left() -> void:
  i -= 1

func move_right() -> void:
  i += 1

func move_by(di: int, dj: int) -> void:
  i += di
  j += dj

func move_to(_i: int, _j: int) -> void:
  i = _i
  j = _j

func can_move_by(di: int, dj: int):
  i += di
  j += dj
  var can = is_in_valid_position()
  i -= di
  j -= dj
  return can

func can_move_left() -> bool:
  return can_move_by(-1, 0)

func can_move_right() -> bool:
  return can_move_by(1, 0)

func can_move_down() -> bool:
  return can_move_by(0, 1)

func is_in_valid_position() -> bool:
  return !is_off_screen() and !is_touching_inactive_blocks()

func is_off_screen() -> bool:
  return i < 0 || i == Constants.TETRIS_POOL_WIDTH || j == Constants.TETRIS_POOL_HEIGHT

func is_touching_inactive_blocks():
  return grid[i][j] != null

func add_to_grid():
  grid[i][j] = self

func _ready():
  if (texture != null):
    spriteNode.texture = texture
  if (color_group != null):
    areaNode.add_to_group(color_group)

func remove_from_grid():
  if grid[i][j] == self:
    grid[i][j] = null
    
func destroy():
  spriteAnimationNode.play("Blink")
  yield(spriteAnimationNode,"animation_finished")
  queue_free()
  emit_signal("block_destroyed")
  
  
