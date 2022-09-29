extends Control

const TWEEN_DURATION = 0.2

enum DialogStates {
  SHOWING,
  SHOWN,
  HIDING,
  HIDDEN,
 }

export(NodePath) var DialogNodePath

onready var ColorRectNode = $ColorRect
onready var GameMenuNode = get_parent() # the parent should be of type GameMenu
onready var DialogNode = get_node(DialogNodePath)
onready var TweenNode = $Tween

onready var shown_pos_y = DialogNode.rect_position.y
onready var hidden_pos_y = shown_pos_y - 1000

var current_state = DialogStates.HIDDEN
var dialog_buttons = []
var last_focus_owner = null #last item that had focus before the dialog pop up

func _ready():
  pause_mode = PAUSE_MODE_PROCESS
  hide_dialog()
  var __ = DialogNode.connect("hide", self, "start_hiding_dialog")
  __ = DialogNode.connect("confirmed", self, "start_hiding_dialog")
  dialog_buttons = _get_dialog_buttons()

func _exit_tree():
  DialogNode.disconnect("hide", self, "start_hiding_dialog")
  DialogNode.disconnect("confirmed", self, "start_hiding_dialog")

func show_dialog():
  if _is_shown_or_showing_state():
    return
  get_tree().paused = true
  prepare_tween(shown_pos_y)
  current_state = DialogStates.SHOWING
  show_nodes()
  last_focus_owner = get_focus_owner()
  dialog_buttons[0].grab_focus()

func show_nodes():
  show()
  DialogNode.show()
  ColorRectNode.show()
  GameMenuNode.handle_back_event = false
    
func hide_dialog():
  DialogNode.rect_position.y = hidden_pos_y
  hide_nodes()
  get_tree().paused = false
  if last_focus_owner != null: last_focus_owner.grab_focus()
  current_state = DialogStates.HIDDEN

func hide_nodes():
  hide()
  DialogNode.hide()
  ColorRectNode.hide()
  GameMenuNode.handle_back_event = true

func _input(_event):
  if _is_accept_or_cancel_pressed() and _is_shown_or_showing_state():
    start_hiding_dialog()

func prepare_tween(target_pos_y):
  TweenNode.remove_all()
  TweenNode.interpolate_property(
    DialogNode,
    "rect_position:y",
    DialogNode.rect_position.y,
    target_pos_y,
    TWEEN_DURATION,
    Tween.TRANS_LINEAR,
    Tween.EASE_IN_OUT)
  TweenNode.start()

func start_hiding_dialog():
  if _is_hidden_or_hiding_state():
    return
  Event.emit_menu_button_pressed(MenuButtons.CONFIRM_DIALOG)
  show_nodes() #just to make sure they are visible
  current_state = DialogStates.HIDING
  prepare_tween(hidden_pos_y)

func _on_Tween_tween_completed(_object, _key):
  if current_state == DialogStates.HIDING:
    hide_dialog()
  elif current_state == DialogStates.SHOWING:
    current_state = DialogStates.SHOWN

func _is_accept_or_cancel_pressed():
  return Input.is_action_just_pressed("ui_cancel") or Input.is_action_just_pressed("ui_accept")

func _is_shown_or_showing_state():
  return current_state == DialogStates.SHOWING or current_state == DialogStates.SHOWN

func _is_hidden_or_hiding_state():
  return current_state == DialogStates.HIDDEN or current_state == DialogStates.HIDING

func _get_dialog_buttons():
  var btns = []
  for ch in DialogNode.get_children():
    if ch is HBoxContainer:
      for chch in ch.get_children():
        if chch is Button:
          btns.append(chch)
      return btns
