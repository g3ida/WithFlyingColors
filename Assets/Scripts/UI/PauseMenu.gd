extends GameMenu

onready var buttons = [
  $CenterContainer/VBoxContainer/ResumeButton,
  $CenterContainer/VBoxContainer/BackButton
]

func ready():
  handle_back_event = false

func hide():
  for b in buttons:
    b.hide()

func show():
  buttons[0].grab_focus()
  for b in buttons:
    b.show()
