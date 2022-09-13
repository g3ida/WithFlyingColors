extends TileMap

signal bricks_cleared()

const Brick = preload("res://Assets/Scenes/BrickBreaker/Brick.tscn")

var color_groups = ["blue", "pink", "purple", "yellow"]
var bricks_count = 0

func fill_grid():
  for i in range(color_groups.size()):
    for cell in get_used_cells_by_id(i):
      var pos = map_to_world(cell)
      set_cell(cell.x, cell.y, -1)
      var brick = Brick.instance()
      brick.color_group = color_groups[i]
      call_deferred("add_child", brick)
      brick.call_deferred("set_owner", self)
      brick.position = pos
      bricks_count += 1

func _ready():
  fill_grid()
      
func connect_signals():
  var __ = Event.connect("brick_broken", self, "_on_brick_broken")
    
func disconnect_signals():
  Event.disconnect("brick_broken", self, "_on_brick_broken")

func _on_brick_broken(_color, _position):
  bricks_count -= 1
  if bricks_count == 0:
    emit_signal("bricks_cleared")
        
func _enter_tree():
  connect_signals()
  
func _exit_tree():
  disconnect_signals()
    
