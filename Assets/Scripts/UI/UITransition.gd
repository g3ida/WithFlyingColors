class_name UITransition
extends Control

signal entered()
signal exited()

enum TransitionStates {
  ENTER_DELAY,
  ENTERING,
  ENTERED,
  EXIT_DELAY,
  EXITING,
  EXITED
}

onready var parent = get_parent()
onready var TweenNode = $Tween

var display_position: Vector2
var hidden_position: Vector2
var current_state = TransitionStates.ENTER_DELAY

export(float) var time = 0.3
export(float) var delay = 0.25
export(Vector2) var hidden_relative_position = Vector2.ZERO

func _ready():
  var __ = parent.connect("ready", self, "_prepare", [], CONNECT_ONESHOT)

#API: enter(), exit()
func enter():
  _enter_delay()

func exit():
  _exit_delay()

func _enter_delay():
  current_state = TransitionStates.ENTER_DELAY
  start_tween(hidden_position, delay)

func _really_enter():
  current_state = TransitionStates.ENTERING
  start_tween(display_position, time)

func _exit_delay():
  current_state = TransitionStates.EXIT_DELAY
  start_tween(display_position, delay)
  
func _really_exit():
  current_state = TransitionStates.EXITING
  start_tween(hidden_position, time)

func start_tween(destination_pos, duration):
  TweenNode.remove_all()
  TweenNode.interpolate_property(
    parent,
    "rect_position",
    parent.rect_position,
    destination_pos,
    duration,
    Tween.TRANS_QUAD,
    Tween.EASE_IN_OUT)
  TweenNode.start()

func _prepare():
  display_position = parent.rect_position
  hidden_position = parent.rect_position + hidden_relative_position
  parent.rect_position = hidden_position

func _on_Tween_tween_completed(_object, _key):
  if current_state == TransitionStates.ENTER_DELAY:
    _really_enter()
    emit_signal("entered")
  elif current_state == TransitionStates.ENTERING:
    current_state = TransitionStates.ENTERED
  elif current_state == TransitionStates.EXIT_DELAY:
    _really_exit()
  elif current_state == TransitionStates.EXITING:
    current_state = TransitionStates.EXITED
    emit_signal("exited")