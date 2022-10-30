extends Node2D

enum State {
  IDLE,
  MOVING,
  MOVED
}

const DURATION = 1.0

signal move_completed(_self)

export var color_group: String setget set_color_group, get_color_group
var current_state = State.IDLE
onready var TweenNode = $Tween
onready var LightNode = $Light2D

func _ready():
  LightNode.visible = false

func set_color_group(_color_group):
  color_group = _color_group
  var color_index = ColorUtils.get_group_color_index(_color_group)
  $GemAnimatedSprite.modulate = ColorUtils.get_basic_color(color_index)
  $Light2D.color = ColorUtils.get_dark_color(color_index)

func get_color_group():
  return color_group

func set_light_intensity(intensity: float):
  LightNode.energy = intensity

func move_to_position(_position: Vector2, wait_time: float, ease_type = 1):
  if current_state == State.IDLE:
    current_state = State.MOVING
    _move_tween(_position, wait_time, ease_type)

func _move_tween(_position: Vector2, wait_time: float, ease_type = 1):
  TweenNode.remove_all()
  TweenNode.interpolate_property(
    self,
    "global_position:x",
    self.global_position.x,
    _position.x,
    DURATION,
    Tween.TRANS_LINEAR,
    ease_type,
    wait_time)
  TweenNode.interpolate_property(
    self,
    "global_position:y",
    self.global_position.y,
    _position.y,
    DURATION,
    Tween.TRANS_CIRC,
    ease_type,
    wait_time)
  TweenNode.start()

func _on_Tween_tween_completed(_object, _key):
  if current_state == State.MOVING:
    current_state = State.MOVED
    emit_signal("move_completed", self)
    LightNode.visible = true
