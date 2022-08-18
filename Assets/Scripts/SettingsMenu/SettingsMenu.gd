extends GameMenu

func process(_delta):
	if Input.is_action_just_pressed("ui_cancel") or Input.is_action_just_pressed("ui_home"):
		navigate_to_screen("res://Assets/Screens/MainMenu.tscn")	

func on_enter():
	animators.append(init_label_animator($GAME, 2*DELAY))
	animators.append(init_label_animator($SETTINGS, 3*DELAY))
	for animator in animators:
		animator.update(0)

func on_exit():
  reverse_animators()

func is_exit_ceremony_done() -> bool:
  return animators_done()

func is_enter_ceremony_done() -> bool:
  return animators_done()
