extends Node2D

const FACE_SEPARATOR_SCALE_FACTOR = 3.5

const BricksTileMap = preload("res://Assets/Scenes/BrickBreaker/BricksTileMap.tscn")
const BouncingBallScene = preload("res://Assets/Scenes/BrickBreaker/BouncingBall.tscn")

onready var DeathZoneNode = $DeathZone
onready var BricksTileMapNode = null
onready var BallsContainer = $BallsContainer
onready var BallSpawnPosNode = $BallsContainer/BallSpawnPos
onready var BricksSpawnPosNode = $BricksContainer/BricksSpawnPos
onready var Checkpoint = $CheckpointArea
onready var TriggerEnterAreaNode = $TriggerEnterArea
onready var SlidingFloorNode = $SlidingFloor
onready var SlidingFloorSliderNode = $SlidingFloor/SlidingPlatform
onready var ProtectionAreaSpawnerPositionNode = $ProtectionAreaSpawnerPosition

var is_playing = false
var num_balls = 0

func spawn_ball(color = "blue"):
  var bouncing_ball = BouncingBallScene.instance()
  bouncing_ball.death_zone = DeathZoneNode
  bouncing_ball.color_group = color
  BallsContainer.call_deferred("add_child", bouncing_ball)
  bouncing_ball.position = BallSpawnPosNode.position
  bouncing_ball.call_deferred("set_owner", BallsContainer)
  num_balls += 1
  return bouncing_ball

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
  __ = Event.connect("bouncing_ball_removed", self, "_on_bouncing_ball_removed")
  
func disconnect_signals():
  Event.disconnect("checkpoint_reached", self, "_on_checkpoint_hit")
  Event.disconnect("checkpoint_loaded", self, "reset")
  Event.disconnect("bouncing_ball_removed", self, "_on_bouncing_ball_removed")

func _enter_tree():
  connect_signals()

func _exit_tree():
  disconnect_signals()

func remove_balls():
  if num_balls == 0: return
  for b in BallsContainer.get_children():
    if b is BouncingBall:
      b.queue_free()
  num_balls = 0
  
func reset():
  if (is_playing):
    remove_balls()
    spawn_ball()
    pass
    
func _on_checkpoint_hit(_checkpoint):
  if Checkpoint == _checkpoint:
    pass

func play():
  if !is_playing:
    is_playing = true
    spawn_ball()
    Global.player.current_default_corner_scale_factor = FACE_SEPARATOR_SCALE_FACTOR

func _on_bouncing_ball_removed(_ball):
  num_balls -= 1
  if num_balls == 0:
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
