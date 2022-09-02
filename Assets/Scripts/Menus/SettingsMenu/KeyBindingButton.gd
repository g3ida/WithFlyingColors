extends Button

const default_text = "(EMPTY)"

export var key: String
var value

var is_listining = false

signal keyboard_action_bound(action, key)

func _ready():
  var action_list = InputMap.get_action_list(key)
  var input_event = Settings.get_first_key_keyboard_event_from_action_list(action_list)
  if input_event != null:
    var input_key_event = input_event as InputEventKey
    value = input_key_event.scancode
    text = OS.get_scancode_string(value)
  $AnimationPlayer.play("RESET")

func undo():
  if value != null:
    text = OS.get_scancode_string(value)
  else:
    text = default_text

func _input(event):
  var handled = false
  if not is_listining:
    return
  if event is InputEventKey:
      value = event.scancode
      text = OS.get_scancode_string(value)
      emit_signal("keyboard_action_bound", key, value)
      handled = true
  elif event is InputEventMouse:
    if event.button_mask & BUTTON_LEFT == BUTTON_LEFT:
      undo()
      handled = true
  if handled:
    pressed = false
    is_listining = false
    get_tree().set_input_as_handled()
    get_tree().paused = false
    $AnimationPlayer.play("RESET")

func is_valid() -> bool:
  return text == default_text

func _on_Control_on_action_bound_signal(_action, _key):
  if _action == self.key or _key != value:
    return
  value = null
  text = default_text
  emit_signal("keyboard_action_bound", _action, null)


func _on_KeyBindingButton_pressed():
  if pressed:
    pressed = true
    is_listining = true
    Event.emit_signal("keyboard_action_biding")
    $AnimationPlayer.play("Blink")
    get_tree().paused = true

func _on_KeyBindingButton_mouse_entered():
  if (!get_tree().paused):
    grab_focus()
