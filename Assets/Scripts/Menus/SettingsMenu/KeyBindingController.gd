extends Control

onready var jump_btn = $GridContainer/JumpBtn
signal on_action_bound_signal(action, key)

func _ready():
  pass

func _on_keyboard_input_action_bound(action, key):
  if key == null:
    Settings.unbind_action_key(action)
  else:
    Settings.bind_action_to_keyboard_key(action, key)
    emit_signal("on_action_bound_signal", action, key)

func on_gain_focus():
  jump_btn.grab_focus()
