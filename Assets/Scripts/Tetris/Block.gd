extends Node2D

const EdgeArea = preload("res://Assets/Scenes/Tetris/EdgeArea.tscn")

export var color_group: String
export var i: int = 0
export var j: int = 0

signal block_destroyed

onready var spriteNode = $BlockSprite
onready var spriteAnimationNode = $BlockSprite/AnimationPlayer
onready var areaNode = $Area2D
onready var areaShapeNode = $Area2D/CollisionShape2D

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

func add_to_grid(permessive_mode = true):
  grid[i][j] = self
  if (permessive_mode):
    add_permessiveness_bounds_if_needed()

func _ready():
  if (color_group != null):
    areaNode.add_to_group(color_group)
    spriteNode.color_group = color_group

func remove_from_grid():
  if grid[i][j] == self:
    grid[i][j] = null
    
func destroy():
  spriteAnimationNode.play("Blink")
  yield(spriteAnimationNode,"animation_finished")
  queue_free()
  emit_signal("block_destroyed")
  
func add_permessiveness_bounds_if_needed():
  var right_edge = i+1 < Constants.TETRIS_POOL_WIDTH\
    and grid[i+1][j] != null\
    and grid[i+1][j].color_group != color_group
      
  var left_edge = i > 0\
    and grid[i-1][j] != null\
    and grid[i-1][j].color_group != color_group
  
  if left_edge:
    add_permessiveness_bounds(DIR_LEFT)
    grid[i-1][j].add_permessiveness_bounds(DIR_RIGHT)
  if right_edge:
    add_permessiveness_bounds(DIR_RIGHT)
    grid[i+1][j].add_permessiveness_bounds(DIR_LEFT)
    
enum {DIR_LEFT = -1, DIR_RIGHT = 1, DIR_BOTH}

func add_permessiveness_bounds(dir):
  var group = grid[i+dir][j].color_group
  var edgeArea = EdgeArea.instance()
  edgeArea.add_to_group(group)
  edgeArea.add_to_group(color_group)
  add_child(edgeArea)
  edgeArea.set_owner(self)

  edgeArea.position.y = areaShapeNode.shape.extents.y
  edgeArea.position.x = edgeArea.width if dir == DIR_LEFT else Constants.TETRIS_BLOCK_SIZE - edgeArea.width
  
  var edge_area_x = edgeArea.collisionShapeNode.shape.extents.x
  var area_x = areaShapeNode.shape.extents.x
  var factor = 1 - (edge_area_x / area_x)
  areaShapeNode.scale.x = factor - (1 - areaShapeNode.scale.x)
  areaShapeNode.position.x -= dir * 0.5 * (1-factor) * Constants.TETRIS_BLOCK_SIZE
