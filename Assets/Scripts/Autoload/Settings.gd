extends Node2D

onready var vsync: bool = OS.vsync_enabled setget setVsync, getVsync
onready var fullscreen: bool = OS.window_fullscreen setget setFullscreen, getFullscreen
onready var window_size: Vector2 = OS.window_size setget setWindowSize, getWindowSize

func setVsync(value: bool):
  vsync = value
  OS.vsync_enabled = vsync

func getVsync() -> bool:
  return vsync

func setFullscreen(value: bool):
	fullscreen = value
	OS.window_fullscreen = fullscreen

func getFullscreen() -> bool:
  return fullscreen

func setWindowSize(value: Vector2):
  window_size = value
  OS.set_window_size(value)

func getWindowSize() -> Vector2:
  return window_size


  
func get_first_key_keyboard_event_from_action_list(action_list: Array) -> InputEvent:
  for el in action_list:
    if el is InputEventKey:
      return el
  return null

func bind_action_to_keyboard_key(action: String, scancode: int):
  # erase the current action:
  var action_list = InputMap.get_action_list(action)
  var input_event = Settings.get_first_key_keyboard_event_from_action_list(action_list)
  if input_event != null:
    var input_key_event = input_event as InputEventKey
    InputMap.action_erase_event(action, input_key_event)
  # add the new action:
  var new_key = InputEventKey.new()
  new_key.set_scancode(scancode)
  InputMap.action_add_event(action, new_key)