class_name TetrisAI

#check https://codemyroad.wordpress.com/2013/04/14/tetris-ai-the-near-perfect-player/

const HEIGHT_WEIGHT: float = 0.510066
const LINES_WEIGHT: float = 0.760666
const HOLES_WEIGHT: float = 0.35663
const BUMPINESS_WEIGHT: float = 0.184483

# grid[col][line]
func _init():
  pass

func clone_grid(grid) -> Array:
  var clone = []
  for i in range(Constants.TETRIS_POOL_WIDTH):
    clone.append([])
    for j in range(Constants.TETRIS_POOL_HEIGHT):
      clone[i].append(grid[i][j])
  return clone

func column_height(grid, c) -> int:
  for i in range(Constants.TETRIS_POOL_HEIGHT):
    if grid[c][i] != null:
      return Constants.TETRIS_POOL_HEIGHT - i
  return 0
    
func aggregate_height(grid):
  var total = 0
  for i in range(Constants.TETRIS_POOL_WIDTH):
    total += column_height(grid, i)
  return total

func is_line(grid, line):
  for i in range(Constants.TETRIS_POOL_WIDTH):
    if grid[i][line] == null:
      return false
  return true

func num_lines(grid):
  var count = 0
  for i in range(Constants.TETRIS_POOL_HEIGHT):
    if is_line(grid, i):
      count += 1
  return count

func num_holes(grid) -> int:
  var count = 0
  for i in range(Constants.TETRIS_POOL_WIDTH):
    var block = false
    for j in range(Constants.TETRIS_POOL_HEIGHT):
      if grid[i][j] != null:
        block = true
      elif grid[i][j] == null and block:
        count += 1
  return count 

func bumpiness(grid):
  var total = 0
  for i in range(Constants.TETRIS_POOL_WIDTH-1):
    total += abs(column_height(grid, i) - column_height(grid, i+1))
  return total

func calculate_grid_score(grid: Array) -> float:
  var cloned = clone_grid(grid)
  remove_full_lines(cloned)

  var height_score = -HEIGHT_WEIGHT * aggregate_height(cloned) 
  var lines_socre = LINES_WEIGHT * num_lines(grid)
  var holes_score = -HOLES_WEIGHT * num_holes(cloned)
  var bumpiness_score = -BUMPINESS_WEIGHT * bumpiness(cloned)
  return height_score + lines_socre + holes_score + bumpiness_score

func best(grid: Array, tetromino: PackedScene):
  var best_rotation = 0
  var best_position = Constants.TETRIS_SPAWN_J
  var best_score = -INF

  for i in range(4): 
    for c in range(Constants.TETRIS_POOL_WIDTH):
      var rotated_piece: Tetromino = tetromino.instance()
      rotated_piece._ready()
      rotated_piece.set_grid(grid)
      rotated_piece.move_by(0, Constants.TETRIS_SPAWN_J)
      #rotate i times
      for _j in range(i):
        rotated_piece.rotate_left()

      if rotated_piece.can_move_by(c, 0):
        rotated_piece.move_by(c, 0)
        while(rotated_piece.move_down_safe()):
          pass
        rotated_piece.add_to_grid()

        var score = calculate_grid_score(grid)
        if score > best_score:
          best_score = score
          best_position = c
          best_rotation = i
        rotated_piece.remove_from_grid()
        rotated_piece.queue_free()
  return {"position": best_position, "rotation": best_rotation, "score": best_score}

# line removal functions
func remove_full_lines(grid):
  var lines = detect_lines(grid)
  for l in lines:
    remove_line_cells(grid, l)

func remove_line_cells(grid, line: int):
  for i in range(Constants.TETRIS_POOL_WIDTH):
    grid[i][line] = null
  move_down_lines_above(grid, line)
    
func move_down_lines_above(grid, line: int):
  for j in range(line-1, -1, -1):
    for i in range(Constants.TETRIS_POOL_WIDTH):
      grid[i][j+1] = grid[i][j]
      grid[i][j] = null

func detect_lines(grid) -> Array:
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
