extends Node2D

onready var vsync: bool = OS.vsync_enabled setget setVsync, getVsync
onready var fullscreen: bool = OS.window_fullscreen setget setFullscreen, getFullscreen
onready var window_size: Vector2 = OS.window_size setget setWindowSize, getWindowSize
onready var sfx_volume: float = get_normalized_audio_bus_volume("sfx") setget setSfxVolume, getSfxVolume
onready var music_volume: float = get_normalized_audio_bus_volume("music") setget setMusicVolume, getMusicVolume

const config_file_path = "settings.ini"
var _is_ready := false

const MAX_VOLUME = 0
const MIN_VOLUME = -50

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

func get_volume_in_dB(volume):
  var vol = (MAX_VOLUME - MIN_VOLUME) * volume + MIN_VOLUME
  return clamp(vol, MIN_VOLUME, MAX_VOLUME)

func get_volume_from_dB(volume_db):
  var vol = -(volume_db / MIN_VOLUME) + 1.0
  return clamp(vol, 0.0, 1.0)

func set_audio_bus_volume(bus_name, volume):
  var vol = get_volume_in_dB(volume)
  var music_bus_index = AudioServer.get_bus_index(bus_name)
  if vol != MIN_VOLUME:
    AudioServer.set_bus_mute(music_bus_index, false)
    AudioServer.set_bus_volume_db(music_bus_index, vol)
  else:
    AudioServer.set_bus_mute(music_bus_index, true)

func get_normalized_audio_bus_volume(bus_name):
  var music_bus_index = AudioServer.get_bus_index(bus_name)
  var volume_db = AudioServer.get_bus_volume_db(music_bus_index)
  return get_volume_from_dB(volume_db)

func setSfxVolume(volume):
  sfx_volume = volume
  set_audio_bus_volume("sfx", volume)

func getSfxVolume():
  return sfx_volume

func setMusicVolume(volume):
  music_volume = volume
  set_audio_bus_volume("music", volume)
  
func getMusicVolume():
  return music_volume

func bind_action_to_keyboard_key(action: String, scancode: int):
  # erase the current action:
  var action_list = InputMap.get_action_list(action)
  var input_event = InputUtils.get_first_key_keyboard_event_from_action_list(action_list)
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
  var input_event = InputUtils.get_first_key_keyboard_event_from_action_list(action_list)
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
    if InputUtils.get_first_key_keyboard_event_from_action_list(action_list) == null:
      return false
  return true

func save_game_settings():
  #save game actions:
  var config_file = ConfigFile.new()

  var game_actions = get_game_actions()
  for key in game_actions:
    var action_list = InputMap.get_action_list(key)
    var key_value = InputUtils.get_first_key_keyboard_event_from_action_list(action_list)
    if key_value != null:
      config_file.set_value("keyboard", key, key_value.scancode)
    else:
      config_file.set_value("keyboard", key, "")

  #display settings
  config_file.set_value("display", "fullscreen", fullscreen)
  config_file.set_value("display", "vsync", vsync)
  config_file.set_value("display", "resolution", String(window_size.x) + "x" + String(window_size.y))
  #audio settings
  config_file.set_value("audio", "sfx_volume", getSfxVolume())
  config_file.set_value("audio", "music_volume", getMusicVolume())

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

    #audio settings
    for key in config_file.get_section_keys("audio"):
      var key_value = config_file.get_value("audio", key)
      if key == "sfx_volume":
        var parsed_float = float(key_value)
        self.sfx_volume = parsed_float
      elif key == "music_volume":
        var parsed_float = float(key_value)
        self.music_volume = parsed_float

  else: #default settings if settings file does not exist:
    self.fullscreen = true
    self.vsync = true
