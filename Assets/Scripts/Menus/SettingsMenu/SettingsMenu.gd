extends GameMenu

onready var tab_container = $TabContainer

func _input(_event):
  if Input.is_action_just_pressed("ui_cancel") or Input.is_action_just_pressed("ui_home"):
    _on_BackButton_pressed()
  elif Input.is_action_just_pressed("ui_left") and tab_container.current_tab == 1:
    tab_container.current_tab = 0
  elif Input.is_action_just_pressed("ui_right") and tab_container.current_tab == 0:
    tab_container.current_tab = 1

func on_enter():
  var __ = Event.connect("Go_to_main_menu_pressed", self, "_on_go_to_main_menu_pressed")
  animators.append(init_control_element_animator($GAME, DELAY))
  animators.append(init_control_element_animator($SETTINGS, 2*DELAY))
  animators.append(init_control_element_animator($BackButton, DELAY))
  for animator in animators:
    animator.update(0)

func on_exit():
  Event.disconnect("Go_to_main_menu_pressed", self, "_on_go_to_main_menu_pressed")
  reverse_animators()

func is_exit_ceremony_done() -> bool:
  return animators_done()

func is_enter_ceremony_done() -> bool:
  return animators_done()

func _on_go_to_main_menu_pressed():
  Settings.save_game_settings()
  navigate_to_screen("res://Assets/Screens/MainMenu.tscn")

func is_valid_state() -> bool:
  return Settings.are_action_keys_valid()

func _on_BackButton_pressed():
  if screen_state == RUNNING:
    if is_valid_state():
      Event.emit_signal("Go_to_main_menu_pressed")
    else:
      pass #fixme: add popup or something
