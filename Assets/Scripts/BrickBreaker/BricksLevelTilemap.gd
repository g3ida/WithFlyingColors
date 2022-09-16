extends TileMap

signal level_bricks_cleared(id)

const Brick = preload("res://Assets/Scenes/BrickBreaker/Brick.tscn")
export var id = 0

var color_groups = ["blue", "pink", "purple", "yellow"]
var bricks_count = 0

onready var parent = get_parent()

func fill_grid():
  for i in range(color_groups.size()):
    for cell in get_used_cells_by_id(i):
      var pos = map_to_world(cell)
      set_cell(cell.x, cell.y, -1)
      var brick = Brick.instance()
      brick.color_group = color_groups[i]
      parent.call_deferred("add_child", brick)
      brick.call_deferred("set_owner", parent)
      var __ = brick.connect("brick_broken", self, "_on_brick_broken")
      brick.position = pos
      bricks_count += 1

func _ready():
  fill_grid()
      
func _on_brick_broken():
  bricks_count -= 1
  if bricks_count == 0:
    emit_signal("level_bricks_cleared", id)
