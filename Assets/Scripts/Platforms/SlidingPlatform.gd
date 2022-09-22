extends Node2D

enum State {WAIT_1, SLIDING_FORTH, WAIT_2, SLIDING_BACK}

export var wait_time: float = 4.0
export var speed: float = 3.0
export var is_stopped = false
export var one_shot = false
export(State) var one_shot_state = State.SLIDING_BACK
export var smooth_landing = false
export var show_gear = true
export var restore_delayed_stop = false #if false, in case of reaching checkpoint when a stop is delayed we consider it as being stopped

onready var platform: KinematicBody2D = get_parent()
onready var tweenNode = $Tween
onready var gearNone = $Gear
onready var follow: Vector2 = platform.global_position
onready var destination: Vector2 = _parse_destination()

var delayed_stop = false
var current_state = State.WAIT_1

var distance: float
var duration: float
var start_pos: Vector2
var end_pos: Vector2

onready var save_data = {
  "state": current_state,
  "position_x": global_position.x,
  "position_y": global_position.y,
  "looping": true,
  "is_stopped": false,
  "delayed_stop": false
}

func _parse_destination() -> Vector2:
  for ch in get_children():
    if ch is Position2D:
      return ch.position * platform.global_scale + follow
  return platform.global_position
  
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

func _get_destination_position():
  if current_state == State.WAIT_1 or current_state == State.SLIDING_BACK:
    return start_pos
  elif current_state == State.SLIDING_FORTH or current_state == State.WAIT_2:
    return end_pos
  return global_position # unkonown state

func _get_source_position():
  if current_state == State.WAIT_2 or current_state == State.SLIDING_BACK:
    return end_pos
  elif current_state == State.SLIDING_FORTH or current_state == State.WAIT_1:
    return start_pos
  return global_position # unkonown state

func _process_tween():
  if is_stopped: return
  tweenNode.remove_all()
  if current_state == State.WAIT_1:
    slide(start_pos, start_pos, wait_time, 0) #wait
  elif current_state == State.SLIDING_FORTH:
    slide(start_pos, end_pos, duration, 0) #slide
  elif current_state == State.WAIT_2:
    slide(end_pos, end_pos, wait_time, 0) #wait
  elif current_state == State.SLIDING_BACK:
    slide(end_pos, start_pos, duration, 0) #slide back

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
  #in case of delayed stop we consider it as being reached
  if delayed_stop and not restore_delayed_stop:
    var dest = _get_destination_position()
    save_data["state"] = _get_next_state(current_state)
    save_data["is_stopped"] = true
    save_data["delayed_stop"] = false
    save_data["position_x"] = dest.x
    save_data["position_y"] = dest.y
    save_data["looping"] = tweenNode.repeat
  else:
    save_data["state"] = current_state
    var dest = _get_source_position()
    save_data["position_x"] = dest.x
    save_data["position_y"] = dest.y
    save_data["looping"] = tweenNode.repeat
    save_data["is_stopped"] = is_stopped
    save_data["delayed_stop"] = delayed_stop

func save():
  return save_data

func reset():
  tweenNode.remove_all()
  current_state = save_data["state"]
  platform.global_position = Vector2(save_data["position_x"], save_data["position_y"])
  follow = platform.global_position
  tweenNode.repeat = save_data["looping"]
  is_stopped = save_data["is_stopped"]
  delayed_stop = save_data["delayed_stop"]
  _process_tween()

#if set to false the slider waits for start or end positions
func stop_slider(stop_directly: bool):
  if is_stopped: return
  if (stop_directly):
    self.is_stopped = true
    tweenNode.remove_all()
  else:
    delayed_stop = true
  
func resume_slider():
  is_stopped = false
  _process_tween()

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

