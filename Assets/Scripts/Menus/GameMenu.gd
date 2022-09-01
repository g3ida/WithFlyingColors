class_name GameMenu
extends Control

const DURATION = 0.3
const DISTANCE = 800.0
const DELAY = 0.25

enum {ENTER, RUNNING, EXIT}

onready var screen_state: int
var destination_screen: String = ""
var animators = []
var current_focus = null

func _enter_tree():
  screen_state = ENTER
  on_enter()
  enter_tree()

func enter_tree():
  pass

func _exit_tree():
  exit_tree()

func exit_tree():
  pass

func _ready():
  ready()

func ready():
  pass

func _process(delta):
  if screen_state == ENTER:
    if is_enter_ceremony_done():
      screen_state = RUNNING
  elif screen_state == EXIT:
    if is_exit_ceremony_done():
      var __ = get_tree().change_scene(destination_screen)
  
  for animator in animators:
    animator.update(delta)
    
  process(delta)

func navigate_to_screen(screen_path: String):
  if screen_state == RUNNING:
    screen_state = EXIT
    destination_screen = screen_path
    on_exit()
  

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
  
