extends GameMenu

onready var MenuBoxNode = $MenuBox
onready var ResetSlotDialogNode = $ResetDialogContainer
onready var CurrentSlotLabelNode = $CurrentSlotLabel

func ready():
  SaveGame.init()

func _ready():
  CurrentSlotLabelNode.text = "Current slot: %d" % (SaveGame.current_slot_index + 1)

func show_reset_data_dialog():
  ResetSlotDialogNode.ShowDialog()

func on_menu_button_pressed(menu_button) -> bool:
  if menu_button == MenuButtons.QUIT:
    if (screen_state == MenuScreenState.ENTERED):
      get_tree().quit()
    return true
  elif menu_button == MenuButtons.PLAY:
    return true
  elif menu_button == MenuButtons.STATS:
    navigate_to_screen(MenuManager.Menus.STATS_MENU)
    return true
  elif menu_button == MenuButtons.SETTINGS:
    navigate_to_screen(MenuManager.Menus.SETTINGS_MENU)
    return true
  elif menu_button == MenuButtons.BACK:
    MenuBoxNode._HideSubMenuIfNeeded()
    return true
  return process_play_sub_menus(menu_button)

func process_play_sub_menus(_menu_button) -> bool:
  if _menu_button == MenuButtons.NEW_GAME:
    if SaveGame.does_slot_have_progress(SaveGame.current_slot_index):
      ResetSlotDialogNode.ShowDialog()
    else:
      navigate_to_screen(MenuManager.Menus.GAME)
      MenuBoxNode._HideSubMenuIfNeeded()
    return true
  if _menu_button == MenuButtons.CONTINUE_GAME:
    MenuBoxNode._HideSubMenuIfNeeded()
    navigate_to_screen(MenuManager.Menus.GAME)
    return true
  if _menu_button == MenuButtons.SELECT_SLOT:
    MenuBoxNode._HideSubMenuIfNeeded()
    navigate_to_screen(MenuManager.Menus.SELECT_SLOT)
    return true
  return false

#signal for ResetDialog
func _on_ResetSlotDialog_confirmed():
  SaveGame.remove_save_slot(SaveGame.current_slot_index)
  MenuBoxNode._HideSubMenuIfNeeded()
  navigate_to_screen(MenuManager.Menus.GAME)
