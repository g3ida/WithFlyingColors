tool
extends Control

export var text = ""
export var id = 0
export(Color) var color
export var event: int
export var disabled = false setget set_disabled, get_disabled

onready var ButtonNode = $Button

func _ready():
  ButtonNode.text = text
  ButtonNode.modulate = color
  focus_mode = Control.FOCUS_NONE
  set_disabled(disabled)
  set_process(false)
  
func update_colors():
  ButtonNode.modulate = color

func button_grab_focus():
  if !disabled: ButtonNode.grab_focus()

func _on_Button_pressed():
  Event.emit_signal("menu_button_pressed", event)

func _on_Button_mouse_entered():
  button_grab_focus()
  
func set_disabled(value):
  disabled = value
  if ButtonNode != null:
    ButtonNode.disabled = value
    if not value:
      ButtonNode.focus_mode = Control.FOCUS_ALL
    else:
      ButtonNode.focus_mode = Control.FOCUS_NONE
  
func get_disabled():
  return disabled
