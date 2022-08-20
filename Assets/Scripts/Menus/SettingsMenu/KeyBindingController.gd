extends Control

onready var jump_key = $GridContainer/JumpKey
signal on_action_bound_signal(action, key)

func _ready():
  pass

func _on_keyboard_input_action_bound(action, key):
  if key == null:
    Settings.unbind_action_key(action)
  else:
    Settings.bind_action_to_keyboard_key(action, key)
    emit_signal("on_action_bound_signal", action, key)
