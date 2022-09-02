extends GameMenu

func init_box_animator(el, delay: float) -> Animator:
  var start = el.rect_position.y + DISTANCE
  var end = el.rect_position.y
  var interpolation = PowInterpolation.new(2)
  var duration = DURATION
  return Animator.new(start, end, funcref(el, "update_position_y"), duration, delay, interpolation)

func ready():
  pass

func process(_delta):
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

func connect_signals():
  var __ = Event.connect("Play_button_pressed", self, "_on_Play_button_pressed")
  __ = Event.connect("Quit_button_pressed", self, "_on_Quit_button_pressed")
  __ = Event.connect("Settings_button_pressed", self, "_on_Settings_button_pressed")
  __ = Event.connect("Stats_button_pressed", self, "_on_Stats_button_pressed")

func disconnect_signals():
  Event.disconnect("Play_button_pressed", self, "_on_Play_button_pressed")
  Event.disconnect("Quit_button_pressed", self, "_on_Quit_button_pressed")
  Event.disconnect("Settings_button_pressed", self, "_on_Settings_button_pressed")
  Event.disconnect("Stats_button_pressed", self, "_on_Stats_button_pressed")
          
func enter_tree():
  connect_signals()
    
func exit_tree():
  disconnect_signals()

func _on_Play_button_pressed():
  navigate_to_screen("res://Levels/Level1.tscn")

func _on_Quit_button_pressed():
  if (screen_state == RUNNING):
    get_tree().quit()

func _on_Settings_button_pressed():
  navigate_to_screen("res://Assets/Screens/SettingsMenu.tscn")
  
func _on_Stats_button_pressed():
  navigate_to_screen("res://Assets/Screens/StatsMenu.tscn")
