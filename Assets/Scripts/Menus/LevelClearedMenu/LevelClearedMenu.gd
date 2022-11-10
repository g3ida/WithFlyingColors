extends GameMenu

func _ready():
  pass

func _input(event):
  #todo: must depend on input type
  if event is InputEventKey:
    Event.emit_menu_button_pressed(MenuButtons.EXIT_LEVEL_CLEAR)

func on_menu_button_pressed(menu_button) -> bool:
  if menu_button == MenuButtons.EXIT_LEVEL_CLEAR:
    navigate_to_screen(MenuManager.Menus.MAIN_MENU)
    return true
  return false
