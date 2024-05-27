extends Node2D

const FACE_SEPARATOR_SCALE_FACTOR = 3.5
const NUM_LEVELS = 2
const LEVELS_Y_GAP = 36 * 4
const LEVELS_WIN_GAP = 2.5 * LEVELS_Y_GAP
const BRIKS_TRANSLATION_Y_AMOUNT = 500.0

const BricksTileMap = preload("res://Assets/Scenes/BrickBreaker/BricksTileMap.tscn")
const BouncingBallScene = preload("res://Assets/Scenes/BrickBreaker/BouncingBall.tscn")

onready var DeathZoneNode = $DeathZone
onready var BricksTileMapNode = null
onready var BallsContainer = $BallsContainer
onready var BallSpawnPosNode = $BallsContainer/BallSpawnPos
onready var BricksSpawnPosNode = $BricksContainer/BricksSpawnPos
onready var BricksTimerNode = $BricksContainer/LevelUpTimer
onready var BricksPowerUpHandler = $BricksContainer/BrickPowerUpHandler
onready var Checkpoint = $CheckpointArea
onready var TriggerEnterAreaNode = $TriggerEnterArea
onready var SlidingFloorNode = $SlidingFloor
onready var SlidingFloorSliderNode = $SlidingFloor/SlidingPlatform
onready var ProtectionAreaSpawnerPositionNode = $ProtectionAreaSpawnerPosition
onready var SlidingDoorNode = $SlidingDoor/SlidingPlatform
onready var CameraLocalizerNode = $CameraLocalizer

var current_state = BrickBreakerState.STOPPED

var num_balls = 0
var current_level = 0

var save_data = {
  "state": current_state
}

var bricksMoveTweener: SceneTreeTween

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
  if BricksTileMapNode != null:
    BricksTileMapNode.disconnect("bricks_cleared", self, "_on_bricks_cleared")
    BricksTileMapNode.disconnect("level_cleared", self, "_on_level_cleared")
    BricksTileMapNode.queue_free()
    BricksTileMapNode = null

func spawn_bricks(should_instance_bricks = true, should_translate_down = true):
  var bricks = BricksTileMap.instance()
  bricks.should_instance_bricks = should_instance_bricks
  bricks.position = BricksSpawnPosNode.position + ((Vector2.UP * BRIKS_TRANSLATION_Y_AMOUNT) if should_translate_down else Vector2.ZERO)
  call_deferred("add_child", bricks)
  bricks.call_deferred("set_owner", self)
  if should_translate_down:
    create_bricks_move_tweener()
    call_deferred("move_bricks_down_by", BRIKS_TRANSLATION_Y_AMOUNT, 5.0)
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

func save():
  return save_data
  
func reset():
  current_state = save_data["state"]
  if current_state == BrickBreakerState.INIT_PLAYING:
    AudioManager.music_track_manager.SetPitchScale(1)
    play()
  elif current_state == BrickBreakerState.WIN:
    if BricksTileMapNode == null:
      BricksTileMapNode = spawn_bricks(false, false)
      BricksTileMapNode.position.y += LEVELS_Y_GAP * NUM_LEVELS + LEVELS_WIN_GAP

func _get_save_state_from_current_state():
  if current_state == BrickBreakerState.LOSE or current_state == BrickBreakerState.PLAYING:
    return BrickBreakerState.INIT_PLAYING
  return current_state
  
func _on_checkpoint_hit(_checkpoint):
  save_data["state"] = _get_save_state_from_current_state()
  if Checkpoint == _checkpoint:
    pass #nothing todo for now

func _on_player_dying(_area, _position, _entity_type):
  if current_state == BrickBreakerState.PLAYING:
    current_state = BrickBreakerState.LOSE
    stop()

func stop():
  remove_balls()
  remove_bricks()
  BricksTimerNode.stop()

func play():
  if current_state != BrickBreakerState.PLAYING:
    current_state = BrickBreakerState.PLAYING
    current_level = 0
    BricksTileMapNode = spawn_bricks()
    Event.emit_brick_breaker_start()
    yield(bricksMoveTweener, "finished")
    spawn_ball()
    BricksTimerNode.start()
    var __ = BricksTileMapNode.connect("bricks_cleared", self, "_on_bricks_cleared")
    __ = BricksTileMapNode.connect("level_cleared", self, "_on_level_cleared")
    Global.player.current_default_corner_scale_factor = FACE_SEPARATOR_SCALE_FACTOR

func _on_bouncing_ball_removed(_ball):
  num_balls -= 1
  if num_balls <= 0:
    Event.emit_signal("player_diying", DeathZoneNode, _ball.global_position, Constants.EntityType.BRICK_BREAKER)  

func _on_TriggerEnterArea_body_entered(body):
  if body != Global.player: return
  if current_state == BrickBreakerState.STOPPED:
    call_deferred("play")
    SlidingFloorSliderNode.set_looping(false)
    SlidingFloorSliderNode.stop_slider(false)
    AudioManager.music_track_manager.LoadTrack("brickBreaker")
    AudioManager.music_track_manager.PlayTrack("brickBreaker")
  
  if current_state == BrickBreakerState.WIN:
    _change_camera_view_after_win()

  if (TriggerEnterAreaNode != null):
    TriggerEnterAreaNode.queue_free()
    TriggerEnterAreaNode = null

func _on_LevelUpTimer_timeout():
  current_level += 1
  if current_level == NUM_LEVELS:
    BricksTimerNode.stop()
  create_bricks_move_tweener()
  move_bricks_down_by(LEVELS_Y_GAP)
  AudioManager.music_track_manager.SetPitchScale(1 + (current_level-1) * 0.1)
  increment_balls_speed()
  
func create_bricks_move_tweener():
  if bricksMoveTweener:
    bricksMoveTweener.kill()
  bricksMoveTweener = create_tween()

func move_bricks_down_by(value: float, duration = 0.25):
  var __ = bricksMoveTweener.tween_property(
    BricksTileMapNode,
    "position:y",
    BricksTileMapNode.position.y + value,
    duration).from(BricksTileMapNode.position.y)

func _on_bricks_cleared():
  if current_state == BrickBreakerState.PLAYING:
    current_state = BrickBreakerState.WIN
    BricksTimerNode.stop()
    Event.emit_break_breaker_win()
    _clean_up_game()
    create_bricks_move_tweener()
    move_bricks_down_by(LEVELS_WIN_GAP, 3.0)
    yield(bricksMoveTweener, "finished")
    AudioManager.music_track_manager.SetPitchScale(1)
    SlidingDoorNode.resume_slider()
    _change_camera_view_after_win()
    Helpers.trigger_functional_checkoint()

func _clean_up_game():
  remove_balls()
  BricksPowerUpHandler.is_active = false
  BricksPowerUpHandler.call_deferred("remove_active_powerups")
  BricksPowerUpHandler.call_deferred("remove_falling_powerups")

func _change_camera_view_after_win():
  CameraLocalizerNode.position_clipping_mode = CamLimitEnum.LIMIT_ALL_BUT_TOP
  CameraLocalizerNode.full_viewport_drag_margin = false
  CameraLocalizerNode.set_camera_limits()
  CameraLocalizerNode.apply_camera_changes()

func _on_level_cleared(level):
  if current_state == BrickBreakerState.PLAYING:
    if current_level != NUM_LEVELS and current_level+1 <= level and BricksTimerNode.time_left > 2.0:
      BricksTimerNode.stop()
      BricksTimerNode.start()
      _on_LevelUpTimer_timeout()
