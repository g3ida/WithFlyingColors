extends Node2D

signal lines_removed(count)
signal game_over()

const s_block =  preload("res://Assets/Scenes/Tetris/S_Block.tscn")
const z_block =  preload("res://Assets/Scenes/Tetris/Z_Block.tscn")
const l_block =  preload("res://Assets/Scenes/Tetris/L_Block.tscn")
const j_block =  preload("res://Assets/Scenes/Tetris/J_Block.tscn")
const o_block =  preload("res://Assets/Scenes/Tetris/O_Block.tscn")
const t_block =  preload("res://Assets/Scenes/Tetris/T_Block.tscn")
const i_block =  preload("res://Assets/Scenes/Tetris/I_Block.tscn")

var tetrominos = [
  s_block,
  z_block,
  l_block,
  j_block,
  o_block,
  t_block,
  i_block
 ]
var random_bag = []
var score = 0

onready var spawnPosNode = $SpawnPosition

export (NodePath) var playerNode

var is_paused: bool = false
var have_active_block = false
var nb_queued_lines_to_remove = 0
var ai: TetrisAI

var shape_is_in_wait_time = false
var move_shape_down_wait_time = 0.01

var block_size: float
var shape: Tetromino

var grid = []

onready var player = get_node(playerNode)

func init_grid():
  grid = []
  for i in range(Constants.TETRIS_POOL_WIDTH):
    grid.append([])
    for _j in range(Constants.TETRIS_POOL_HEIGHT):
      grid[i].append(null)

func get_random_tetromino_with_next():
  if random_bag.size() > 1:
    var current = random_bag.pop_back()
    var next = random_bag.back()
    return {"current": current, "next": next}
  elif random_bag.empty():
    random_bag = [ s_block, z_block, l_block, j_block, o_block, t_block, i_block ]
    #random_bag = [ t_block, t_block ]
    #########
    random_bag.shuffle()
    return get_random_tetromino_with_next()
  else:
    var current = random_bag.pop_back()
    random_bag = [ s_block, z_block, l_block, j_block, o_block, t_block, i_block ]
    #random_bag = [ t_block, t_block ]
#############
    random_bag.shuffle()
    random_bag.push_front(current)
    return get_random_tetromino_with_next()

func get_random_tetromino():
  if !random_bag.empty():
    return random_bag.pop_back()
  else:
    random_bag = [ s_block, z_block, l_block, j_block, o_block, t_block, i_block ]
    random_bag.shuffle()
    return random_bag.pop_back()

func ai_spawn_block():
  var pick = get_random_tetromino_with_next()
  var current_tetromino = pick["current"]
  var best = ai.best(grid, current_tetromino)
  var pos = best["position"]
  var rot = best["rotation"]

  shape = current_tetromino.instance()
  shape.set_grid(grid)
  shape.move_by(pos, Constants.TETRIS_SPAWN_J)
  add_child(shape)
  for _i in range(rot):
    shape.rotate_left()
  shape.position = spawnPosNode.position + Vector2(Constants.TETRIS_BLOCK_SIZE*(pos-Constants.TETRIS_SPAWN_I), 0)
  return shape

func default_spawn_block():
  var random_tetromino = get_random_tetromino()
  shape = random_tetromino.instance()
  shape.set_grid(grid)
  shape.move_by(Constants.TETRIS_SPAWN_I, Constants.TETRIS_SPAWN_J)
  add_child(shape)
  shape.position = spawnPosNode.position
  return shape

func _generate_blocks():
  have_active_block = true
  
  shape = ai_spawn_block()

  if (!shape.can_move_down()):
    is_paused = true
    emit_signal("game_over")

func _physics_process(_delta):
  
  if (is_paused or nb_queued_lines_to_remove > 0):
    return
  
  if !have_active_block:
    _generate_blocks()
  
  if (shape != null and !shape_is_in_wait_time):
      move_shape_down()

func move_shape_down():
  shape_is_in_wait_time = true
  if shape.move_down_safe():
    yield(get_tree().create_timer(move_shape_down_wait_time), "timeout")
  else:
    shape.add_to_grid()
    remove_lines()
    have_active_block = false
  shape_is_in_wait_time = false
 
func remove_lines():
  var lines = detect_lines()
  emit_signal("lines_removed", lines.size())
  for l in lines:
    remove_line_cells(l)

func remove_line_cells(line: int):
  nb_queued_lines_to_remove += 1
  var animation_duration = grid[0][line].blink_animation_duration
  for i in range(Constants.TETRIS_POOL_WIDTH):
    grid[i][line].destroy()
    grid[i][line] = null
  yield(get_tree().create_timer(animation_duration), "timeout")
  move_down_lines_above(line)
  nb_queued_lines_to_remove -= 1


func move_down_lines_above(line: int):
  for j in range(line-1, -1, -1):
    for i in range(Constants.TETRIS_POOL_WIDTH):
      if grid[i][j] != null:
        grid[i][j].j += 1
        grid[i][j].position.y += Constants.TETRIS_BLOCK_SIZE
      if grid[i][j+1] != null:
        grid[i][j+1].queue_free()
      #override cell
      grid[i][j+1] = grid[i][j]
      grid[i][j] = null

func detect_lines() -> Array:
  var lines_to_remove := []
  for j in range(Constants.TETRIS_POOL_HEIGHT):
    var complete_line := true
    for i in range(Constants.TETRIS_POOL_WIDTH):
      if grid[i][j] == null:
        complete_line = false
        break
    if complete_line:
      lines_to_remove.append(j)
  return lines_to_remove

func _ready():
  randomize()
  init_grid()
  ai = TetrisAI.new()

func reset():
  pass

func _on_TetrixPool_lines_removed(count):
  score += count

func _on_TetrixPool_game_over():
  pass
