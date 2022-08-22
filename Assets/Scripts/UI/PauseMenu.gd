extends Control

onready var buttons = [
  $CenterContainer/VBoxContainer/BackButton,
  $CenterContainer/VBoxContainer/ResumeButton
]

func _ready():
  pass

func hide():
  for b in buttons:
    b.hide()

func show():
  buttons[0].grab_focus()
  for b in buttons:
    b.show()
