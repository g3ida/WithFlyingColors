extends Node2D

enum State {WAIT_1, SLIDING_FORTH, WAIT_2, SLIDING_BACK}

export var wait_time: float = 1.0
export var speed: float = 3.0
export var is_stopped = false
export var one_shot = false
export(State) var one_shot_state = State.SLIDING_BACK
export var smooth_landing = true
export var show_gear = true

onready var platform: KinematicBody2D = get_parent()
onready var tweenNode = $Tween
onready var gearNone = $Gear
onready var follow: Vector2 = platform.global_position
onready var destination: Vector2 = _parse_destination()

#saved params
var saved_looping = true
var saved_is_stopped = false
var is_saved = false
var delayed_stop = false
var current_state = State.WAIT_1

var distance: float
var duration: float
var start_pos: Vector2
var end_pos: Vector2

func _parse_destination() -> Vector2:
  for ch in get_children():
    if ch is Position2D:
      return ch.position * platform.global_scale + follow
  return global_position
  
func _setup():
  tweenNode.repeat = false
  distance = (destination - follow).length()
  duration = distance / float(speed*Global.WORLD_TO_SCREEN)
  start_pos = follow
  end_pos = destination
  if !show_gear: gearNone.visible = false
  
func _ready():
  _setup()
  _process_tween()

func _physics_process(_delta):
  if smooth_landing:
    platform.global_position = platform.global_position.linear_interpolate(follow, 0.075)
  else:
    platform.global_position = follow

func _process_tween():
  if is_stopped: return

  if current_state == State.WAIT_1:
    slide(start_pos, start_pos, wait_time, 0) #wait
  elif current_state == State.SLIDING_FORTH:
    slide(start_pos, end_pos, duration, 0) #slide
  elif current_state == State.WAIT_2:
    slide(end_pos, end_pos, wait_time, 0) #wait
  elif current_state == State.SLIDING_BACK:
    slide(end_pos, start_pos, duration, 0) #slide back

    tweenNode.start()

func slide(start: Vector2, end: Vector2, _duration: float, wait: float):
  tweenNode.interpolate_property(
    self,
    "follow",
    start,
    end,
    _duration,
    Tween.TRANS_LINEAR,
    Tween.EASE_IN_OUT,
    wait)
  tweenNode.start()
  
func set_looping(looping: bool):
  tweenNode.repeat = looping

func connect_signals():
  var __ = Event.connect("checkpoint_reached", self, "_on_checkpoint_hit")
  __ = Event.connect("checkpoint_loaded", self, "reset")
  
func disconnect_signals():
  Event.disconnect("checkpoint_reached", self, "_on_checkpoint_hit")
  Event.disconnect("checkpoint_loaded", self, "reset")
      
func _enter_tree():
  connect_signals()

func _exit_tree():
  disconnect_signals()
  
func _on_checkpoint_hit(_checkpoint):
  saved_looping = tweenNode.repeat
  saved_is_stopped = is_stopped or delayed_stop
  is_saved = true
  
#if set to false the slider waits for start or end positions
func stop_slider(stop_directly: bool):
  if is_stopped: return
  if (stop_directly):
    self.is_stopped = true
    self.tweenNode.stop_all()
  else:
    delayed_stop = true
  
func resume_slider():
  is_stopped = false
  _process_tween()
  
func reset():
  if is_saved:
    tweenNode.repeat = saved_looping
    if is_stopped and not saved_is_stopped:
       resume_slider()

func _get_next_state(state):
  if state == State.WAIT_1:
    return State.SLIDING_FORTH
  if state == State.SLIDING_FORTH:
    return State.WAIT_2
  if state == State.WAIT_2:
    return State.SLIDING_BACK
  if state == State.SLIDING_BACK:
    return State.WAIT_1
  return null

func _on_Tween_tween_completed(_object, _key):
  current_state = _get_next_state(current_state)
  if delayed_stop:
    delayed_stop = false
    stop_slider(true)
  if one_shot and current_state == one_shot_state:
    delayed_stop = true
  _process_tween()

