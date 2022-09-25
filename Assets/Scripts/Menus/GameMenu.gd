class_name GameMenu
extends Control

const DURATION = 0.3
const DISTANCE = 900.0
const DELAY = 0.25

enum {ENTER, RUNNING, EXIT}

onready var screen_state: int
var destination_screen: int
var animators = []
var current_focus = null
var handle_back_event = true

func _enter_tree():
  screen_state = ENTER
  var __ = Event.connect("menu_button_pressed", self, "_on_menu_button_pressed")
  set_process(true)
  on_enter()
  enter_tree()

func enter_tree():
  pass

func _exit_tree():
  set_process(false)
  Event.disconnect("menu_button_pressed", self, "_on_menu_button_pressed")
  exit_tree()

func exit_tree():
  pass

func _ready():
  ready()

func ready():
  pass

func _process(delta):
  if handle_back_event and (Input.is_action_just_pressed("ui_cancel") or Input.is_action_just_pressed("ui_home")):
    Event.emit_menu_button_pressed(MenuButtons.BACK)
  if screen_state == ENTER:
    if is_enter_ceremony_done():
      screen_state = RUNNING
  elif screen_state == EXIT:
    if is_exit_ceremony_done():
      MenuManager.go_to_menu(destination_screen)
  for animator in animators:
    animator.update(delta)
  process(delta)

func navigate_to_screen(menu_screen):
  if screen_state == RUNNING:
    screen_state = EXIT
    destination_screen = menu_screen
    on_exit()
  
func _on_menu_button_pressed(menu_button):
  if not on_menu_button_pressed(menu_button):
    if menu_button == MenuButtons.BACK:
      navigate_to_screen(MenuManager.previous_menu)

func on_menu_button_pressed(_menu_button) -> bool:
  return false

func process(_delta):
  var focus_owner = get_focus_owner()
  if (focus_owner != null && focus_owner != self.current_focus):
    Event.emit_signal("focus_changed")
  self.current_focus = focus_owner

func on_enter():
  pass

func on_exit():
  pass

func is_exit_ceremony_done() -> bool:
  return true

func is_enter_ceremony_done() -> bool:
  return true

#animators
func init_control_element_animator(el, delay: float) -> Animator:
  var start = el.rect_position.x - DISTANCE
  var end = el.rect_position.x    
  var interpolation = PowInterpolation.new(2)
  var duration = DURATION
  return Animator.new(start, end, funcref(el, "update_position_x"), duration, delay, interpolation)

func animators_done() -> bool:
  for animator in animators:
    if animator.is_running():
      return false
  return true
  
func reverse_animators():
  for animator in animators:
    animator.reset()
    animator.reverse()
  
