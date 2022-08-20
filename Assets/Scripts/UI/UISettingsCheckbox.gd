tool
extends HBoxContainer

onready var label = $Label
onready var is_on = false

func _ready():
	label.text = "OFF" #Fixme: set this from settings
	is_on = false

func _on_CheckBox_toggled(button_pressed):
	is_on = !is_on
	label.text = "ON" if is_on else "OFF"
