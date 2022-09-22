extends GameMenu

onready var BackButtonNode = $BackButton

func process(_delta):
  pass

func on_enter():
  animators.append(init_control_element_animator($BackButton, DELAY))
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