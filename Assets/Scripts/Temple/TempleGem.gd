extends Node2D

enum State {
  IDLE,
  MOVING,
  MOVED
}

const DURATION = 1.0

signal move_completed(_self)

export var color_group: String setget set_color_group, get_color_group
onready var LightNode = $Light2D

var current_state = State.IDLE
var tweener: SceneTreeTween

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
  if tweener:
    tweener.kill()
  tweener = create_tween()
  var __ = tweener.connect("finished", self, "_on_tween_completed", [], CONNECT_ONESHOT)
  __ = tweener.set_parallel(true)
  __ = tweener.tween_property(
    self,
    "global_position:x",
    _position.x,
    DURATION
  ).set_trans(Tween.TRANS_LINEAR).set_ease(ease_type).set_delay(wait_time)
  __ = tweener.tween_property(
    self,
    "global_position:y",
    _position.y,
    DURATION
  ).set_trans(Tween.TRANS_CIRC).set_ease(ease_type).set_delay(wait_time)

func _on_tween_completed():
  if current_state == State.MOVING:
    current_state = State.MOVED
    emit_signal("move_completed", self)
    LightNode.visible = true
