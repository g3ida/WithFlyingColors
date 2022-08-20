extends Control

onready var jump_key = $GridContainer/JumpKey

func _ready():
	pass

func _on_keyboard_input_action_bound(action, key):
	Settings.bind_action_to_keyboard_key(action, key)
