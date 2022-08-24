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

var current_state := WAITING_SOURCE
enum {WAITING_SOURCE, MOVING_TO_TARGET, WAITING_TARGET, MOVING_TO_SOURCE}

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

  slide(start_pos, end_pos, duration, wait_time)
  slide(end_pos, start_pos, duration, duration + wait_time * 2)
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
