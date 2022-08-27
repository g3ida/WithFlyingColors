extends GameMenu

onready var buttons = [
  $CenterContainer/VBoxContainer/ResumeButton,
  $CenterContainer/VBoxContainer/BackButton
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
