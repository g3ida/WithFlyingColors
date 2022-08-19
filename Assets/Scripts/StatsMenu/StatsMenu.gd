extends GameMenu

func process(_delta):
	if Input.is_action_just_pressed("ui_cancel") or Input.is_action_just_pressed("ui_home"):
		Event.emit_signal("Go_to_main_menu_pressed")	

func on_enter():
	Event.connect("Go_to_main_menu_pressed", self, "_on_go_to_main_menu_pressed")
	animators.append(init_control_element_animator($GAME, DELAY))
	animators.append(init_control_element_animator($STATS, 2*DELAY))
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
	navigate_to_screen("res://Assets/Screens/MainMenu.tscn")


func _on_BackButton_pressed():
	navigate_to_screen("res://Assets/Screens/MainMenu.tscn")
