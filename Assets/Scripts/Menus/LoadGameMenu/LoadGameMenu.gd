extends GameMenu

onready var BackButtonNode = $BackButton
onready var LoadTextNode = $LOAD
onready var SlotsContainer = $SlotsContainer
onready var DialogContainerNode = $DialogContainer
  
func _on_BackButton_pressed():
  Event.emit_menu_button_pressed(MenuButtons.BACK)

func _on_SlotsContainer_slot_pressed(id):
  if SaveGame.is_slot_filled(id):
    SaveGame.current_slot_index = id
    Event.emit_menu_button_pressed(MenuButtons.LOAD_GAME)
  else:
    Event.emit_menu_button_pressed(MenuButtons.SHOW_DIALOG)

func on_menu_button_pressed(menu_button):
  if menu_button == MenuButtons.LOAD_GAME:
    navigate_to_screen(MenuManager.Menus.GAME)
    return true
  if menu_button == MenuButtons.SHOW_DIALOG:
    DialogContainerNode.show_dialog()
    return true
  return false
