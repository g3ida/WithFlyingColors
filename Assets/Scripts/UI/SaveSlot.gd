extends PanelContainer

signal pressed(action)

enum State {INIT, DEFAULT, FOCUS, ACTIONS_SHOWN}

const MOVE_DURATION = 0.1
const FOCUS_SHIFT = 0
const FOCUS_OFF_BG_COLOR = Color(0.1765, 0.1765, 0.1765, 0.079)
const FOCUS_ON_BG_COLOR = Color(0.1765, 0.1765, 0.1765, 0.196)
const MIN_WIDTH = 1160

var texture: ImageTexture = null setget set_texture, get_texture #Was used before in the old design.
var timestamp: int = -1 setget set_timestamp, get_timestamp
var description: String = "" setget set_description, get_description
var bg_color: Color = FOCUS_OFF_BG_COLOR setget set_bg_color, get_bg_color
var has_focus: bool setget set_has_focus, get_has_focus
var is_disabled: bool = false setget set_is_disabled, get_is_disabled
var id: int = 0 setget set_slot_index_label, get_slot_index_label
var current_state = State.INIT

onready var DescriptionNode = $HBoxContainer/VBoxContainer/Description
onready var TimestampNode = $HBoxContainer/VBoxContainer/Timestamp
onready var ContainerNode = $HBoxContainer
onready var VBoxContainerNode = $HBoxContainer/VBoxContainer
onready var SlotIndexNode = $HBoxContainer/VBoxContainer/SlotIndex
onready var ActionButtonsNode = $HBoxContainer/ActionButtons
onready var ButtonNode = $Button
onready var AnimationPlayerNode = $AnimationPlayer

onready var pos_x = rect_position.x

func _ready():
  set_texture(texture)
  set_timestamp(timestamp)
  set_description(description)
  set_slot_index_label(id)
  set_bg_color(bg_color)
  set_state(State.DEFAULT)
  self.rect_min_size.x = MIN_WIDTH

func set_texture(value):
  texture = value
  
func get_texture():
  return texture

func set_description(value):
  description = value
  DescriptionNode.text = description

func get_description():
  return description

func set_slot_index_label(value):
  id = value
  ActionButtonsNode.slot_index = id
  SlotIndexNode.text = "SLOT %d" % (value + 1)

func get_slot_index_label():
  return id

func set_timestamp(value):
  timestamp = value
  if timestamp == -1:
    TimestampNode.text = "----/--/-- --:--"
  else:
    var time = Time.get_datetime_dict_from_unix_time(value)
    var display_string : String = "%d/%02d/%02d %02d:%02d" % [time.year, time.month, time.day, time.hour, time.minute];
    TimestampNode.text = display_string

func get_timestamp():
  return timestamp

func set_bg_color(value):
  bg_color = value

func get_bg_color():
  return bg_color

func _on_Button_pressed():
  if current_state == State.FOCUS:
    set_state(State.ACTIONS_SHOWN)
  elif current_state == State.ACTIONS_SHOWN:
    set_state(State.FOCUS)
    emit_signal("pressed", "focus")
  
func _process(_delta):
  if get_has_focus():
    set_bg_color(FOCUS_ON_BG_COLOR)
    AnimationPlayerNode.play("Blink")
  else:
    set_bg_color(FOCUS_OFF_BG_COLOR)
    AnimationPlayerNode.play("RESET")
    
func _on_Button_mouse_entered():
  ButtonNode.grab_focus()

func set_has_focus(value):
  if value:
    ButtonNode.grab_focus()
  else:
    pass

func get_has_focus():
  if ButtonNode.has_focus():
    if current_state == State.DEFAULT:
      set_state(State.FOCUS)
    return true
  else:
    if current_state == State.ACTIONS_SHOWN and (not ActionButtonsNode.buttons_has_focus()):
      set_state(State.DEFAULT)
    return false
  
func set_is_disabled(value):
  is_disabled = value
  ButtonNode.disabled = value
  if value:
    ButtonNode.focus_mode = Control.FOCUS_NONE
  else:
    ButtonNode.focus_mode = Control.FOCUS_ALL
  
func get_is_disabled():
  return is_disabled

func set_state(new_state):
  if new_state == current_state: return
  if new_state == State.DEFAULT:
    AnimationPlayerNode.play("RESET")
    ActionButtonsNode.hide_button()
    set_bg_color(FOCUS_OFF_BG_COLOR)
    hide_button_node(false)
  elif new_state == State.FOCUS:
    AnimationPlayerNode.play("Blink")
    set_bg_color(FOCUS_ON_BG_COLOR)
    hide_button_node(false)
    ActionButtonsNode.hide_button()
  elif new_state == State.ACTIONS_SHOWN:
    if current_state == State.DEFAULT:
      set_state(State.FOCUS)
    hide_button_node(true)
    ActionButtonsNode.show_button()
  #at the end update the current_state to be the new_state
  current_state = new_state
  
func hide_button_node(hide):
  if (hide):
    ButtonNode.visible = false
    ButtonNode.disabled = true
  else:
    ButtonNode.visible = true
    ButtonNode.disabled = false


func _on_ActionButtons_clear_button_pressed(_slot_index):
  emit_signal("pressed", "delete")

func _on_ActionButtons_select_button_pressed(_slot_index):
  emit_signal("pressed", "select")
  hide_action_buttons()

func hide_action_buttons():
  set_has_focus(true)
  set_state(State.FOCUS)

func update_meta_data():
  var meta_data = SaveGame.get_slot_meta_data(id)
  if meta_data != null:
    set_timestamp(meta_data["save_time"])
    # fixme: make this description dynamic
    set_description("LEVEL: 10 - Progress: %d%%" % meta_data["progress"])
  else:
    set_timestamp(-1)
    set_description("<EMPTY>")

func set_border(state: bool):
  if (state):
    var greyPanelWithBorder = load("res://Assets/Styles/greyPanelWithBorder.tres")
    remove_stylebox_override("panel")
    add_stylebox_override("panel", greyPanelWithBorder)
  else:
    var greyPanelWithBorderTransparent = load("res://Assets/Styles/greyPanelWithBorderTransparent.tres")
    remove_stylebox_override("panel")
    add_stylebox_override("panel", greyPanelWithBorderTransparent)

