extends GameMenu

onready var BackButtonNode = $BackButton
onready var SaveTextNode = $SAVE
onready var SlotsContainer = $SlotsContainer
onready var DialogContainerNode = $DialogContainer
onready var TextNode = $Text

func on_enter():
  animators.append(init_control_element_animator($BackButton, 2*DELAY))
  animators.append(init_control_element_animator($SAVE, DELAY))
  animators.append(init_slots_animator(DELAY))
  for animator in animators:
    animator.update(0)

func on_exit():
  reverse_animators()

func is_exit_ceremony_done() -> bool:
  return animators_done()

func is_enter_ceremony_done() -> bool:
  return animators_done()
  
func _on_BackButton_pressed():
  Event.emit_menu_button_pressed(MenuButtons.BACK)
  
func update_slots_y_pos(pos_y):
  $SlotsContainer.rect_position.y = pos_y

func _on_SlotsContainer_slot_pressed(id):
  if not SaveGame.is_slot_filled(id):
    SaveGame.current_slot_index = id
    Event.emit_menu_button_pressed(MenuButtons.SAVE_GAME)
  else:
    SaveGame.current_slot_index = id
    Event.emit_menu_button_pressed(MenuButtons.SHOW_DIALOG)

func on_menu_button_pressed(menu_button):
  if menu_button == MenuButtons.SAVE_GAME:
    navigate_to_screen(MenuManager.Menus.GAME)
    return true
  if menu_button == MenuButtons.SHOW_DIALOG:
    DialogContainerNode.show_dialog()
    return true
  return false


func _on_FilledSlotDialog_confirmed():
  SaveGame.remove_save_slot(SaveGame.current_slot_index)
  navigate_to_screen(MenuManager.Menus.GAME)
