extends Node2D

const FACE_SEPARATOR_SCALE_FACTOR = 3.5
const NUM_LEVELS = 2
const LEVELS_Y_GAP = 36 * 4

const BricksTileMap = preload("res://Assets/Scenes/BrickBreaker/BricksTileMap.tscn")
const BouncingBallScene = preload("res://Assets/Scenes/BrickBreaker/BouncingBall.tscn")

onready var DeathZoneNode = $DeathZone
onready var BricksTileMapNode = null
onready var BallsContainer = $BallsContainer
onready var BallSpawnPosNode = $BallsContainer/BallSpawnPos
onready var BricksSpawnPosNode = $BricksContainer/BricksSpawnPos
onready var BricksTimerNode = $BricksContainer/LevelUpTimer
onready var BricksMoveTweenNode = $BricksContainer/BricksMoveTween
onready var Checkpoint = $CheckpointArea
onready var TriggerEnterAreaNode = $TriggerEnterArea
onready var SlidingFloorNode = $SlidingFloor
onready var SlidingFloorSliderNode = $SlidingFloor/SlidingPlatform
onready var ProtectionAreaSpawnerPositionNode = $ProtectionAreaSpawnerPosition

enum State {PLAYING, PAUSED, LOSE, STOPPED, WIN}
var current_state = State.STOPPED

var num_balls = 0
var current_level = 0

func spawn_ball(color = "blue"):
  var bouncing_ball = BouncingBallScene.instance()
  bouncing_ball.death_zone = DeathZoneNode
  bouncing_ball.color_group = color
  BallsContainer.call_deferred("add_child", bouncing_ball)
  bouncing_ball.position = BallSpawnPosNode.position
  bouncing_ball.call_deferred("set_owner", BallsContainer)
  bouncing_ball.call_deferred("set_color", color)
  num_balls += 1
  return bouncing_ball

func remove_bricks():
  BricksTileMapNode.disconnect("bricks_cleared", self, "_on_bricks_cleared")
  BricksTileMapNode.disconnect("level_cleared", self, "_on_level_cleared")
  BricksTileMapNode.queue_free()

func spawn_bricks():
  var bricks = BricksTileMap.instance()
  bricks.position = BricksSpawnPosNode.position
  call_deferred("add_child", bricks)
  bricks.call_deferred("set_owner", self)
  return bricks

func _ready():
  pass

func connect_signals():
  var __ = Event.connect("checkpoint_reached", self, "_on_checkpoint_hit")
  __ = Event.connect("checkpoint_loaded", self, "reset")
  __ = Event.connect("player_diying", self, "_on_player_dying")
  __ = Event.connect("bouncing_ball_removed", self, "_on_bouncing_ball_removed")
  
func disconnect_signals():
  Event.disconnect("checkpoint_reached", self, "_on_checkpoint_hit")
  Event.disconnect("checkpoint_loaded", self, "reset")
  Event.disconnect("player_diying", self, "_on_player_dying")
  Event.disconnect("bouncing_ball_removed", self, "_on_bouncing_ball_removed")

func _enter_tree():
  connect_signals()

func _exit_tree():
  disconnect_signals()

func increment_balls_speed():
  for b in BallsContainer.get_children():
    if b is BouncingBall:
      b.increment_speed()

func remove_balls():
  for b in BallsContainer.get_children():
    if b is BouncingBall:
      b.queue_free()
  num_balls = 0
  
func reset():
  if current_state == State.LOSE:
    current_state = State.PLAYING
    stop()
    play()
    
func _on_checkpoint_hit(_checkpoint):
  if Checkpoint == _checkpoint:
    pass

func _on_player_dying(_area, _position, _entity_type):
  if current_state == State.PLAYING:
    current_state = State.LOSE

func stop():
  remove_balls()
  remove_bricks()
  BricksTimerNode.stop()

func play():
  current_level = 0
  spawn_ball()
  BricksTimerNode.start()
  BricksTileMapNode = spawn_bricks()
  var __ = BricksTileMapNode.connect("bricks_cleared", self, "_on_bricks_cleared")
  __ = BricksTileMapNode.connect("level_cleared", self, "_on_level_cleared")
  Global.player.current_default_corner_scale_factor = FACE_SEPARATOR_SCALE_FACTOR

func _on_bouncing_ball_removed(_ball):
  num_balls -= 1
  if num_balls == 0:
    current_state = State.LOSE
    Event.emit_signal("player_died")  

func _on_TriggerEnterArea_body_entered(body):
  if body != Global.player: return
  play()
  SlidingFloorSliderNode.set_looping(false)
  SlidingFloorSliderNode.stop_slider(false)
  #AudioManager.music_track_manager.add_track("tetris", "res://Assets/Music/Myuu-Tetris-Dark-Version.mp3", -5.0)
  #AudioManager.music_track_manager.play_track("tetris")
  if (TriggerEnterAreaNode != null):
    TriggerEnterAreaNode.queue_free()
    TriggerEnterAreaNode = null

func _on_LevelUpTimer_timeout():
  current_level += 1
  if current_level == NUM_LEVELS:
    BricksTimerNode.stop()
  move_bricks_down_by(LEVELS_Y_GAP)
  increment_balls_speed()

func move_bricks_down_by(value: float):
  BricksMoveTweenNode.interpolate_property(
    BricksTileMapNode,
    "position:y",
    BricksTileMapNode.position.y,
    BricksTileMapNode.position.y + value,
    0.15)
  BricksMoveTweenNode.start()

func _on_bricks_cleared():
  if current_state == State.PLAYING:
    current_state = State.WIN
    Event.emit_break_breaker_win()
    remove_balls()
    move_bricks_down_by(2*LEVELS_Y_GAP)

func _on_level_cleared(level):
  if current_state == State.PLAYING:
    if level != NUM_LEVELS and current_level+1 <= level and BricksTimerNode.time_left < 2.0:
      BricksTimerNode.stop()
      BricksTimerNode.start()
      _on_LevelUpTimer_timeout()
