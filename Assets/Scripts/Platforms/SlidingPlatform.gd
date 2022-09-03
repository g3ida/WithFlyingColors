extends Node2D

export var wait_time: float = 1.0
export var is_vertical: bool = false
export var speed: float = 3.0
export var distance: float = 350.0

onready var platform: KinematicBody2D = get_parent()
onready var tweenNode = $Tween
onready var gearNone = $Gear

var direction: Vector2
var follow: Vector2

#saved params
var saved_looping = true
var saved_is_stopped = false
var is_saved = false

var is_stopped = false
var delayed_stop = false


func _ready():
  direction = Vector2.UP if is_vertical else Vector2.RIGHT
  follow = platform.position
  _init_tween()

func _physics_process(_delta):
  platform.position = platform.position.linear_interpolate(follow, 0.075)

func _init_tween():
  var duration = distance / float(speed*Global.WORLD_TO_SCREEN)
  var start_pos = platform.position
  var end_pos = start_pos + direction * distance

  #waits are added just for signals
  slide(start_pos, start_pos, wait_time, 0) #wait
  slide(start_pos, end_pos, duration, wait_time) #slide
  slide(end_pos, end_pos, wait_time, duration + wait_time) #wait
  slide(end_pos, start_pos, duration, duration + wait_time * 2) #slide back
  tweenNode.start()

func slide(start: Vector2, end: Vector2, duration: float, wait: float):
  tweenNode.interpolate_property(
    self,
    "follow",
    start,
    end,
    duration,
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
  _init_tween()
  
func reset():
  if is_saved:
    tweenNode.repeat = saved_looping
    if is_stopped and not saved_is_stopped:
       resume_slider()
  
func _on_Tween_tween_completed(_object, _key):
  if delayed_stop:
    delayed_stop = false
    stop_slider(true)
