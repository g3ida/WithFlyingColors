extends Node2D

const DURATION = 0.1
const EXPAND_SIZE = 100
const REDUCE_SIZE = 0

enum CutsceneState {
  DISABLED,
  ENABLING,
  ENABLED,
  DISABLING,
}

var current_cutscene_id = null
var current_state = CutsceneState.DISABLED

onready var CanvasNode = $CanvasLayer
onready var TweenNode = $Tween
onready var TopRectNode = $CanvasLayer/Control/TopRect
onready var BottomRectNode = $CanvasLayer/Control/BottomRect
onready var TimerNode = $Timer

onready var bottom_reduce_position = BottomRectNode.rect_position.y
onready var bottom_expand_position = BottomRectNode.rect_position.y - EXPAND_SIZE

func _ready():
  pass

func _enter_tree():
  var __ = Event.connect("cutscene_request_start", self, "_on_cutscene_request_start")
  __ = Event.connect("cutscene_request_end", self, "_on_cutscene_request_end")

func _exit_tree():
  Event.disconnect("cutscene_request_start", self, "_on_cutscene_request_start")
  Event.disconnect("cutscene_request_end", self, "_on_cutscene_request_end")

func is_busy():
  return current_state != CutsceneState.DISABLED

func _is_disabling_or_disabled_state():
  return current_state != CutsceneState.DISABLED or current_state != CutsceneState.DISABLING

func _is_enabled_or_enabling_state():
  return current_state != CutsceneState.ENABLED or current_state != CutsceneState.ENABLING

func _on_cutscene_request_start(id: String):
  if current_state == CutsceneState.DISABLED or current_state == CutsceneState.DISABLING:
    current_state = CutsceneState.ENABLING
    current_cutscene_id = id
    CanvasNode.visible = true
    Global.player.handle_input_is_disabled = true
    _show_stripes()
    
func _on_cutscene_request_end(id: String):
  if _is_enabled_or_enabling_state() and current_cutscene_id == id:
    current_state = CutsceneState.DISABLING
    _hide_stripes()

func _show_stripes():
  TweenNode.remove_all()
  _start_tween(TopRectNode, EXPAND_SIZE)
  _start_bottom_tween(BottomRectNode, bottom_expand_position)

func _hide_stripes():
  TweenNode.remove_all()
  _start_tween(TopRectNode, REDUCE_SIZE)
  _start_bottom_tween(BottomRectNode, bottom_reduce_position)

func _start_tween(control_node: Control, dest_size):
  TweenNode.interpolate_property(
    control_node,
    "rect_size:y",
    control_node.rect_size.y,
    dest_size,
    DURATION,
    Tween.TRANS_QUAD,
    Tween.EASE_IN_OUT)
  TweenNode.start()

func _start_bottom_tween(control_node: Control, dest_position):
  TweenNode.interpolate_property(
    control_node,
    "rect_position:y",
    control_node.rect_position.y,
    dest_position,
    DURATION,
    Tween.TRANS_QUAD,
    Tween.EASE_IN_OUT)
  TweenNode.start()

func _on_Tween_tween_completed(_object, _key):
  if current_state == CutsceneState.DISABLING:
    current_state = CutsceneState.DISABLED
    CanvasNode.visible = false
    Global.player.handle_input_is_disabled = false

func _show_some_node(node: Node2D, duration = 7.0, move_speed = 3.2):
    var camera_last_focus = Global.camera.follow
    var camera_last_speed = Global.camera.smoothing_speed
    Event.emit_cutscene_request_start("my_cutsecne")
    if node != null: Global.camera.follow = node
    Global.camera.smoothing_speed = move_speed
    TimerNode.wait_time = duration*0.6; TimerNode.start(); yield(TimerNode,"timeout")
    #set focus back so the camera goes back to the previous node
    Global.camera.follow = camera_last_focus
    TimerNode.wait_time = duration*0.4; TimerNode.start(); yield(TimerNode,"timeout")
    Event.emit_cutscene_request_end("my_cutsecne")
    Global.camera.smoothing_speed = camera_last_speed
