extends GameMenu

onready var MenuBoxNode = $MenuBox

func init_box_animator(el, delay: float) -> Animator:
  var start = el.rect_position.y + DISTANCE
  var end = el.rect_position.y
  var interpolation = PowInterpolation.new(2)
  var duration = DURATION
  return Animator.new(start, end, funcref(el, "update_position_y"), duration, delay, interpolation)

func ready():
  pass
  
func on_enter():
  animators.append(init_control_element_animator($WITH, DELAY))
  animators.append(init_control_element_animator($FLYING, 2*DELAY))
  animators.append(init_control_element_animator($COLORS, 3*DELAY))
  animators.append(init_box_animator($MenuBox, 2*DELAY))

  for animator in animators:
    animator.update(0)

func on_exit():
  reverse_animators()

func is_exit_ceremony_done() -> bool:
  return animators_done()
  
func is_enter_ceremony_done() -> bool:
  return animators_done()

func on_menu_button_pressed(menu_button) -> bool:
  if menu_button == MenuButtons.QUIT:
    if (screen_state == RUNNING):
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
  #TODO FILL THIS SHIT
  return false

