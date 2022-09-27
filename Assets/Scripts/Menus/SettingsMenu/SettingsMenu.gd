extends GameMenu

onready var tab_container = $TabContainer
onready var DialogContainerNode = $DialogContainer

func _input(_event):
  if Input.is_action_just_pressed("ui_left") and tab_container.current_tab == 1:
    tab_container.current_tab = 0
  elif Input.is_action_just_pressed("ui_right") and tab_container.current_tab == 0:
    tab_container.current_tab = 1
    
func on_menu_button_pressed(_menu_button) -> bool:
  if _menu_button == MenuButtons.SHOW_DIALOG:
    DialogContainerNode.show_dialog()
    return true
  elif _menu_button == MenuButtons.BACK:
    Settings.save_game_settings()
    # we don't return true here because we want the default behaviour to be called
  return false

func is_valid_state() -> bool:
  return Settings.are_action_keys_valid()

func _on_BackButton_pressed():
  if not is_in_transition_state():
    if is_valid_state():
      Event.emit_menu_button_pressed(MenuButtons.BACK)
    else:
      Event.emit_menu_button_pressed(MenuButtons.SHOW_DIALOG)
