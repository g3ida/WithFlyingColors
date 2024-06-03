extends GameMenu

onready var BackButtonNode = $BackButton
onready var SlotsContainer = $SlotsContainer
onready var ResetDialogContainerNode = $ResetDialogContainer
onready var NoSelectedSlotDialogContainer = $NoSelectedSlotDialogContainer
onready var CurrentSlotLabelNode = $CurrentSlotLabel
onready var current_slot_on_focus = SaveGame.current_slot_index

var delete_tmp_id = 0 #used to save the currently deleting slot

func _ready():
  SlotsContainer.SetGameCurrentSelectedSlot(SaveGame.current_slot_index)
  set_selected_slot_label()

func _on_BackButton_pressed():
  if SaveGame.current_slot_index == -1:
    Event.emit_menu_button_pressed(MenuButtons.SHOW_DIALOG)
    #NoSelectedSlotDialogContainer.ShowDialog()
  else:
    Event.emit_menu_button_pressed(MenuButtons.BACK)
   
func on_menu_button_pressed(_menu_button) -> bool:
  if _menu_button == MenuButtons.SHOW_DIALOG:
    NoSelectedSlotDialogContainer.ShowDialog()
    return true
  elif _menu_button == MenuButtons.DELETE_SLOT:
    return true
  elif _menu_button == MenuButtons.SELECT_SLOT:
    return true
  elif _menu_button == MenuButtons.BACK:
    return false # we don't return true here because we want the default behaviour to be called
  return false

func update_slots_y_pos(pos_y):
  $SlotsContainer.rect_position.y = pos_y

func _on_SlotsContainer_slot_pressed(id, action):
  print("Slot pressed: ", id, action)
  current_slot_on_focus = id
  if action == "select":
    SaveGame.current_slot_index = id
    _on_confirm_slot_button_selected(id)
    SlotsContainer.SetGameCurrentSelectedSlot(id)
    Event.emit_menu_button_pressed(MenuButtons.SELECT_SLOT)
  elif action == "delete":
    delete_tmp_id = id
    ResetDialogContainerNode.ShowDialog()
    Event.emit_menu_button_pressed(MenuButtons.DELETE_SLOT)
  elif action == "focus":
    pass

func _on_confirm_slot_button_selected(_slot_index):
  if SaveGame.is_slot_filled(_slot_index):
    SaveGame.current_slot_index = _slot_index
  else:
    SaveGame.save(_slot_index, true)
    SaveGame.refresh()
  set_selected_slot_label()

func _on_ResetSlotDialog_confirmed():
  SaveGame.remove_save_slot(current_slot_on_focus)
  SaveGame.refresh()
  SlotsContainer.UpdateSlot(delete_tmp_id, true)
  SlotsContainer.SetGameCurrentSelectedSlot(SaveGame.current_slot_index)
  set_selected_slot_label()

func set_selected_slot_label():
  if SaveGame.current_slot_index != -1:
    CurrentSlotLabelNode.text = "%d" % (SaveGame.current_slot_index + 1)
  else:
    CurrentSlotLabelNode.text = "None" 
