extends Node2D

onready var vsync: bool = OS.vsync_enabled setget setVsync, getVsync
onready var fullscreen: bool = OS.window_fullscreen setget setFullscreen, getFullscreen
onready var window_size: Vector2 = OS.window_size setget setWindowSize, getWindowSize

const config_file_path = "settings.ini"
var _is_ready := false

func _ready():
  set_process(false)
  load_game_settings()
  _is_ready = true

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

func unbind_action_key(action: String): 
  # erase the current action:
  var action_list = InputMap.get_action_list(action)
  var input_event = Settings.get_first_key_keyboard_event_from_action_list(action_list)
  if input_event != null:
    var input_key_event = input_event as InputEventKey
    InputMap.action_erase_event(action, input_key_event)

func get_game_actions() -> Array:
  var actions = InputMap.get_actions()
  var game_actions = []
  for action in actions:
    if action.find("ui_") == -1:
      game_actions.append(action)
  return game_actions

func are_action_keys_valid() -> bool:
  var game_actions = get_game_actions()
  for action in game_actions:
    var action_list = InputMap.get_action_list(action)
    if get_first_key_keyboard_event_from_action_list(action_list) == null:
      return false
  return true

func save_game_settings():
  #save game actions:
  var config_file = ConfigFile.new()

  var game_actions = get_game_actions()
  for key in game_actions:
    var action_list = InputMap.get_action_list(key)
    var key_value = get_first_key_keyboard_event_from_action_list(action_list)
    if key_value != null:
      config_file.set_value("keyboard", key, key_value.scancode)
    else:
      config_file.set_value("keyboard", key, "")

  config_file.set_value("display", "fullscreen", fullscreen)
  config_file.set_value("display", "vsync", vsync)
  config_file.set_value("display", "resolution", String(window_size.x) + "x" + String(window_size.y))
  config_file.save(config_file_path)

func load_game_settings():
  var config_file = ConfigFile.new()
  if config_file.load(config_file_path) == OK:
    for key in config_file.get_section_keys("keyboard"):
      var key_value = config_file.get_value("keyboard", key)
      if str(key_value) != "":
        bind_action_to_keyboard_key(key, key_value)

    for key in config_file.get_section_keys("display"):
      var key_value = config_file.get_value("display", key)
      if key == "fullscreen":
        var parsed_boolean = bool(key_value)
        if parsed_boolean != null: 
          self.fullscreen = parsed_boolean
      elif key == "vsync":
        var parsed_boolean = bool(key_value)
        if parsed_boolean != null: 
          self.vsync = vsync
      elif key == "resolution":
        var values = key_value.split("x")
        if values.size() == 2:
          self.window_size = Vector2(float(values[0]), float(values[1]))
  else: #default settings if settings file does not exist:
    self.fullscreen = true
    self.vsync = true
