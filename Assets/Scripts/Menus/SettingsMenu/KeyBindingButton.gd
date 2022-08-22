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
    if event.scancode == KEY_ESCAPE:
      undo()
      handled = true
    else:
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
    get_tree().paused = false
    get_tree().set_input_as_handled()
    $AnimationPlayer.play("RESET")

func is_valid() -> bool:
  return text == default_text

func _on_Control_on_action_bound_signal(action, key):
  if action == self.key or key != value:
    return
  value = null
  text = default_text
  emit_signal("keyboard_action_bound", action, null)


func _on_KeyBindingButton_pressed():
  if pressed:
    pressed = true
    is_listining = true
    $AnimationPlayer.play("Blink")
    get_tree().paused = true
