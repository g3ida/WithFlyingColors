tool
extends Control

export var text = ""
export var id = 0
export(Color) var color
export var event: int

onready var ButtonNode = $Button

func _ready():
  ButtonNode.text = text
  ButtonNode.modulate = color

func update_colors():
  ButtonNode.modulate = color

func button_grab_focus():
  ButtonNode.grab_focus()

func _on_Button_pressed():
  Event.emit_signal("menu_button_pressed", event)


func _on_Button_mouse_entered():
  button_grab_focus()
