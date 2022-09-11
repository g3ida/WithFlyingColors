extends TileMap

const Brick = preload("res://Assets/Scenes/BrickBreaker/Brick.tscn")

var color_groups = ["blue", "pink", "purple", "yellow"]

func fill_grid():
  for i in range(color_groups.size()):
    for cell in get_used_cells_by_id(i):
      var pos = map_to_world(cell)
      set_cell(cell.x, cell.y, -1)
      var brick = Brick.instance()
      brick.color_group = color_groups[i]
      add_child(brick)
      brick.position = pos

func _ready():
  fill_grid()
      
func _enter_tree():
  pass
