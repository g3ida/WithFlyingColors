extends Control

export var SelectDriverScript: Script

var select_driver
var index: int
var selected_value

onready var ui_label = $UISelect/Label
onready var ui_left = $UISelect/Left
onready var ui_right = $UISelect/Right

signal Value_changed(value)

func _ready():
	select_driver = SelectDriverScript.new()
	index = select_driver.get_default_selected_index()
	update_selected_item()

func _on_Left_pressed():
	index = (index + 1) % select_driver.items.size()
	update_selected_item()
	emit_signal("Value_changed", select_driver.item_values[index])

func _on_Right_pressed():
	index = (index - 1) % select_driver.items.size()
	update_selected_item()
	emit_signal("Value_changed", select_driver.item_values[index])

func update_selected_item():
	ui_label.text = select_driver.items[index]
	select_driver.on_item_selected(ui_label.text)
	selected_value = select_driver.item_values[index]
