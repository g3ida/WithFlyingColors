extends GameMenu

onready var tab_container = $TabContainer

func _input(_event):
  if Input.is_action_just_pressed("ui_left") and tab_container.current_tab == 1:
    tab_container.current_tab = 0
  elif Input.is_action_just_pressed("ui_right") and tab_container.current_tab == 0:
    tab_container.current_tab = 1

func on_enter():
  animators.append(init_control_element_animator($GAME, DELAY))
  animators.append(init_control_element_animator($SETTINGS, 2*DELAY))
  animators.append(init_control_element_animator($BackButton, DELAY))
  for animator in animators:
    animator.update(0)

func on_exit():
  reverse_animators()

func is_exit_ceremony_done() -> bool:
  return animators_done()

func is_enter_ceremony_done() -> bool:
  return animators_done()

func on_menu_button_pressed(_menu_button) -> bool:
  Settings.save_game_settings()
  return false

func is_valid_state() -> bool:
  return Settings.are_action_keys_valid()

func _on_BackButton_pressed():
  if screen_state == RUNNING:
    if is_valid_state():
      Event.emit_menu_button_pressed(MenuButtons.BACK)
    else:
      pass #fixme: add popup or something
