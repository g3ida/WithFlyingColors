extends Node2D

signal lines_removed(count)
signal game_over()

const level_up = preload("res://Assets/Scenes/Tetris/LevelUp.tscn")

const s_block =  preload("res://Assets/Scenes/Tetris/S_Block.tscn")
const z_block =  preload("res://Assets/Scenes/Tetris/Z_Block.tscn")
const l_block =  preload("res://Assets/Scenes/Tetris/L_Block.tscn")
const j_block =  preload("res://Assets/Scenes/Tetris/J_Block.tscn")
const o_block =  preload("res://Assets/Scenes/Tetris/O_Block.tscn")
const t_block =  preload("res://Assets/Scenes/Tetris/T_Block.tscn")
const i_block =  preload("res://Assets/Scenes/Tetris/I_Block.tscn")

const tetrominos = [
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
var level = 1
var high_score = 40

onready var spawnPosNode = $SpawnPosition
onready var scoreBoardNode = $ScoreBoard
onready var shapeWaitTimerNode = $ShapeWaitTimer
onready var removeLinesDurationTimerNode = $RemoveLinesDurationTimer
onready var nextPieceNode = $NextPiece
onready var levelUpPositionNode = $LevelUpPosition
onready var SlidingFloorSliderNode = $SlidingFloor/SlidingPlatform
onready var TriggerEnterAreaNode = $TriggerEnterArea

export (NodePath) var playerNode

var is_paused: bool = false
var have_active_block = false
var nb_queued_lines_to_remove = 0
var ai: TetrisAI
var shape_is_in_wait_time = false
var shape: Tetromino
var grid = []
var is_vergin := true

onready var player = get_node(playerNode)

func clear_grid():
  if grid != null:
    for i in range(Constants.TETRIS_POOL_WIDTH):
      for j in range(Constants.TETRIS_POOL_HEIGHT):
        if grid[i][j] != null:
          grid[i][j].queue_free()
    grid = null

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
    random_bag.shuffle()
    return get_random_tetromino_with_next()
  else:
    var current = random_bag.pop_back()
    random_bag = [ s_block, z_block, l_block, j_block, o_block, t_block, i_block ]
    random_bag.shuffle()
    random_bag.push_back(current)
    return get_random_tetromino_with_next()

func ai_spawn_block():
  var pick = get_random_tetromino_with_next()
  var current_tetromino = pick["current"]
  nextPieceNode.set_next_piece(pick["next"])
  var best = ai.best(grid, current_tetromino)
  var pos = best["position"]
  var rot = best["rotation"]

  shape = current_tetromino.instance()
  shape.set_grid(grid)
  shape.move_by(pos, Constants.TETRIS_SPAWN_J)
  add_child(shape)
  shape.set_owner(self)
  for _i in range(rot):
    shape.rotate_left()
  shape.position = spawnPosNode.position + Vector2(Constants.TETRIS_BLOCK_SIZE*(pos-Constants.TETRIS_SPAWN_I), 0)
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
    shapeWaitTimerNode.start()
    yield(shapeWaitTimerNode, "timeout")
  else:
    shape.add_to_grid()
    remove_lines()
    have_active_block = false
  shape_is_in_wait_time = false
 
func remove_lines():
  var lines = detect_lines()
  if (lines.size() > 0):
    emit_signal("lines_removed", lines.size())
    Event.emit_signal("tetris_lines_removed")
  for l in lines:
    remove_line_cells(l)

func remove_line_cells(line: int):
  nb_queued_lines_to_remove += 1
  removeLinesDurationTimerNode.wait_time = grid[0][line].blink_animation_duration
  for i in range(Constants.TETRIS_POOL_WIDTH):
    grid[i][line].destroy()
    grid[i][line] = null
  removeLinesDurationTimerNode.start()
  yield(removeLinesDurationTimerNode, "timeout")
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
  reset(true)

func _enter_tree():
  connect_signals()
  
func _exit_tree():
  disconnect_signals()

func connect_signals():
  var __ = Event.connect("player_diying", self, "_on_player_diying")
  __ = Event.connect("checkpoint_loaded", self, "reset")
    
func disconnect_signals():
  Event.disconnect("player_diying", self, "_on_player_diying")
  Event.disconnect("checkpoint_loaded", self, "reset")

func reset(first_time = false):
  if (is_vergin and !first_time):
    return
  is_paused = true
  nb_queued_lines_to_remove = 0
  score = 0
  have_active_block = false
  shape_is_in_wait_time = false
  shapeWaitTimerNode.wait_time = Constants.TETRIS_SPEEDS[0]
  random_bag = []
  if shape != null:
    shape.queue_free()
    shape = null
  if !first_time:
    clear_grid()
    is_paused = false
  init_grid()
  update_scorebaord()

func update_scorebaord():
  scoreBoardNode.set_high_score(high_score)
  scoreBoardNode.set_score(score)
  var old_level = level
  level = int(score / 10)+1
  if (old_level != level):
    scoreBoardNode.set_level(level)
    var speed = min(level, Constants.TETRIS_MAX_LEVELS)
    shapeWaitTimerNode.wait_time = Constants.TETRIS_SPEEDS[speed]
    AudioManager.music_track_manager.set_pitch_scale(1 + (speed-1) * 0.1)
    if (level > 1):
      var level_up_node = level_up.instance()
      add_child(level_up_node)
      level_up_node.set_owner(self)
      level_up_node.position = levelUpPositionNode.position


func _on_player_diying(_area, _position, _entity_type):
  is_paused = true

func _on_TetrixPool_lines_removed(count):
  score += count
  update_scorebaord()

func _on_TetrixPool_game_over():
  pass
  
func _on_TriggerEnterArea_body_entered(_body):
  if _body != Global.player: return
  Global.camera.zoom_by(1.5)
  is_paused = false
  SlidingFloorSliderNode.set_looping(false)
  SlidingFloorSliderNode.stop_slider(false)
  is_vergin = false
  
  AudioManager.music_track_manager.add_track("tetris", "res://Assets/Music/Myuu-Tetris-Dark-Version.mp3", -5.0)
  AudioManager.music_track_manager.play_track("tetris")
  
  if (TriggerEnterAreaNode != null):
    TriggerEnterAreaNode.queue_free()
    TriggerEnterAreaNode = null
