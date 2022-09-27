extends GameMenu

onready var MenuBoxNode = $MenuBox

func ready():
  SaveGame.init()
  
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
    MenuBoxNode._hide_sub_menu_if_needed()
    return true
  return process_play_sub_menus(menu_button)

func process_play_sub_menus(_menu_button) -> bool:
  if _menu_button == MenuButtons.NEW_GAME:
    if SaveGame.has_filled_slots:
      navigate_to_screen(MenuManager.Menus.SAVE_MENU)
    else:
      navigate_to_screen(MenuManager.Menus.GAME)
    MenuBoxNode._hide_sub_menu_if_needed()
    return true
  if _menu_button == MenuButtons.CONTINUE_GAME:
    SaveGame.current_slot_index = SaveGame.get_most_recent_saved_slot_index()
    MenuBoxNode._hide_sub_menu_if_needed()
    navigate_to_screen(MenuManager.Menus.GAME)
    return true
  if _menu_button == MenuButtons.LOAD_GAME:
    MenuBoxNode._hide_sub_menu_if_needed()
    navigate_to_screen(MenuManager.Menus.LOAD_MENU)
    return true
  return false

