extends GameMenu

onready var buttons = [
  $CenterContainer/VBoxContainer/ResumeButton,
  $CenterContainer/VBoxContainer/BackButton
]

func ready():
  handle_back_event = false
  Global.pause_menu = self

func hide():
  for b in buttons:
    b.hide()

func show():
  buttons[0].grab_focus()
  for b in buttons:
    b.show()

func go_to_main_menu():
  navigate_to_screen(MenuManager.Menus.MAIN_MENU)
