class_name GameMenu
extends Control

enum MenuScreenState {ENTERING, ENTERED, EXITING, EXITED}

const LEVEL_SCREEN_IDX = -1

var screen_state: int
var destination_screen: int
var destination_scene_path: String
var current_focus = null
var handle_back_event = true

var transition_elements = []
var entered_transition_elements_count = 0

func _enter_tree():
  _connect_signals()
  _parse_transition_elems()
  if _has_no_transition_elems():
    screen_state = MenuScreenState.ENTERED
  else:
    screen_state = MenuScreenState.ENTERING
  on_enter()

func _exit_tree():
  _disconnect_signals()
  on_exit()

func _ready():
  _enter_transition_elements()
  ready()

func ready():
  pass

func _process(delta):
  var focus_owner = get_focus_owner()
  if (focus_owner != null && focus_owner != self.current_focus):
    Event.emit_signal("focus_changed")
  self.current_focus = focus_owner
  process(delta)

func _input(_event):
  if screen_state == MenuScreenState.ENTERING or screen_state == MenuScreenState.EXITING:
    get_tree().set_input_as_handled()
  if handle_back_event and (Input.is_action_just_pressed("ui_cancel") or Input.is_action_just_pressed("ui_home")):
    Event.emit_menu_button_pressed(MenuButtons.BACK)

func navigate_to_screen(menu_screen):
  if screen_state == MenuScreenState.ENTERING or MenuScreenState.ENTERED:
    destination_screen = menu_screen
    if _has_no_transition_elems():
      screen_state = MenuScreenState.EXITED
      MenuManager.go_to_menu(destination_screen)
    else:
      screen_state = MenuScreenState.EXITING
      _stop_process_input()
      _exit_transition_elements()
    on_exit()

func navigate_to_level_screen(level_screen_path):
  if level_screen_path == null:
    return
  MenuManager.set_current_level(level_screen_path)
  navigate_to_screen(MenuManager.Menus.GAME)
  
func _on_menu_button_pressed(menu_button):
  if not on_menu_button_pressed(menu_button):
    if menu_button == MenuButtons.BACK:
      navigate_to_screen(MenuManager.previous_menu)

func on_menu_button_pressed(_menu_button) -> bool:
  return false

func process(_delta):
  pass

func on_enter():
  pass

func on_exit():
  pass

func _connect_signals():
  var __ = Event.connect("menu_button_pressed", self, "_on_menu_button_pressed")

func _disconnect_signals():
  Event.disconnect("menu_button_pressed", self, "_on_menu_button_pressed")

func _parse_transition_elems():
  transition_elements = []
  for ch in get_children():
    for chch in ch.get_children():
      if chch is UITransition:
        transition_elements.append(chch)
        var __ = chch.connect("entered", self, "_transition_element_entered")
        __ = chch.connect("exited", self, "_transition_element_exited")
        break

func clear_transition_elems():
  for ch in transition_elements:
    ch.disconnect("entered", self, "_transition_element_entered")
    ch.disconnect("exited", self, "_transition_element_exited")

func _enter_transition_elements():
  for el in transition_elements:
    el.enter()

func _exit_transition_elements():
  for el in transition_elements:
    el.exit()

func is_in_transition_state():
  return screen_state != MenuScreenState.ENTERED

func _has_no_transition_elems():
  return transition_elements.size() == 0

func _transition_element_entered():
  entered_transition_elements_count += 1
  if entered_transition_elements_count == transition_elements.size():
    screen_state = MenuScreenState.ENTERED
    
func _transition_element_exited():
  entered_transition_elements_count -= 1
  if entered_transition_elements_count == 0:
    screen_state = MenuScreenState.EXITED
    if (destination_screen == LEVEL_SCREEN_IDX):
      MenuManager.go_to_screen(destination_scene_path)
    else:
      MenuManager.go_to_menu(destination_screen)
    
func _stop_process_input(node = self):
  for ch in node.get_children():
    ch.set_process_input(false)
    _stop_process_input(ch)
