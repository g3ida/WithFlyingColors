extends Node

signal play_sfx(sfx_name)

const MusicTrackManager = preload("res://Assets/Scenes/Audio/MusicTrackManager.tscn")

onready var jump_sfx: AudioStreamPlayer = AudioStreamPlayer.new()
onready var rotate_left_sfx: AudioStreamPlayer = AudioStreamPlayer.new()
onready var rotate_right_sfx: AudioStreamPlayer = AudioStreamPlayer.new()

var sfx_data: Dictionary = {
  "jump": {"path": "res://Assets/sfx/jumping.ogg", "volume": -5},
  "rotateLeft": {"path": "res://Assets/sfx/rotatex.ogg", "volume": -20, "pitch_scale": 0.9},
  "rotateRight": {"path": "res://Assets/sfx/rotatex.ogg", "volume":  -20},
  "menuSelect": {"path": "res://Assets/sfx/menu_select.ogg", "volume": 0},
  "menuValueChange": {"path": "res://Assets/sfx/click.ogg", "volume": 0},
  "menuMove": {"path": "res://Assets/sfx/menu_move.ogg", "volume": 0},
  "menuFocus": {"path": "res://Assets/sfx/click2.ogg"},
  "land": {"path": "res://Assets/sfx/stand.ogg", "volume": -8},
  "gemCollect": {"path": "res://Assets/sfx/gem.ogg", "volume": -15},
  "playerExplode": {"path": "res://Assets/sfx/die.ogg", "volume": -10},
  "playerFalling": {"path": "res://Assets/sfx/falling.ogg", "volume": -10},
  "tetrisLine": {"path": "res://Assets/sfx/tetris_line.ogg", "volume": -7},
}

var sfx_pool: Dictionary = {}
var music_track_manager = null

func fill_sfx_pool():
  for key in sfx_data.keys():
    #fixme check for nulls
    var stream = load(sfx_data[key]["path"])
    var audio_player := AudioStreamPlayer.new()
    audio_player.stream = stream
    audio_player.stream.set_loop(false)

    if sfx_data[key].has("volume"):
      audio_player.volume_db = float(sfx_data[key]["volume"])
            
    if sfx_data[key].has("pitch_scale"):
      audio_player.pitch_scale = sfx_data[key]["pitch_scale"]
      
    audio_player.bus = "sfx"
    sfx_pool[key] = audio_player
    add_child(audio_player)

func _ready():
  pause_mode = PAUSE_MODE_PROCESS
  music_track_manager = MusicTrackManager.instance()
  add_child(music_track_manager)
  fill_sfx_pool()

func _sfx_play(sfx):
  # fixme: add safe access
  sfx_pool[sfx].play()

func connect_signals():
  var __ = self.connect("play_sfx", self, "_sfx_play")
  __ = Event.connect("player_jumped", self, "_on_player_jumped")
  __ = Event.connect("player_rotate", self, "_on_player_rotate")
  __ = Event.connect("player_land", self, "_on_player_land")
  __ = Event.connect("Play_button_pressed", self, "_on_menu_pressed")
  __ = Event.connect("Stats_button_pressed", self, "_on_menu_pressed")
  __ = Event.connect("Settings_button_pressed", self, "_on_menu_pressed")
  __ = Event.connect("Quit_button_pressed", self, "_on_menu_pressed")
  __ = Event.connect("Go_to_main_menu_pressed", self, "_on_menu_pressed")
  __ = Event.connect("gem_collected", self, "_on_gem_collected")
  __ = Event.connect("Fullscreen_toggled", self, "_on_button_toggle")
  __ = Event.connect("Vsync_toggled", self, "_on_button_toggle")
  __ = Event.connect("Screen_size_changed", self, "_on_button_toggle")
  __ = Event.connect("on_action_bound", self, "_on_key_bound")
  __ = Event.connect("tab_changed", self, "_on_tab_changed")
  __ = Event.connect("focus_changed", self, "_on_focus_changed")
  __ = Event.connect("menu_box_rotated", self, "_on_menu_box_rotated")
  __ = Event.connect("keyboard_action_biding", self, "_on_keyboard_action_biding")
  __ = Event.connect("player_explode", self, "_on_player_explode")
  __ = Event.connect("pause_menu_enter", self, "_on_pause_menu_enter")
  __ = Event.connect("pause_menu_exit", self, "_on_pause_menu_exit")
  __ = Event.connect("player_fall", self, "_on_player_falling")
  __ = Event.connect("tetris_lines_removed", self, "_on_tetris_lines_removed")



func disconnect_signals():
  self.disconnect("play_sfx", self, "_sfx_play")
  Event.disconnect("player_jumped", self, "_on_player_jumped")
  Event.disconnect("player_rotate", self, "_on_player_rotate")
  Event.disconnect("player_land", self, "_on_player_land")
  Event.disconnect("Play_button_pressed", self, "_on_menu_pressed")
  Event.disconnect("Stats_button_pressed", self, "_on_menu_pressed")
  Event.disconnect("Settings_button_pressed", self, "_on_menu_pressed")
  Event.disconnect("Quit_button_pressed", self, "_on_menu_pressed")
  Event.disconnect("Go_to_main_menu_pressed", self, "_on_menu_pressed")
  Event.disconnect("gem_collected", self, "_on_gem_collected")
  Event.disconnect("Fullscreen_toggled", self, "_on_button_toggle")
  Event.disconnect("Vsync_toggled", self, "_on_button_toggle")
  Event.disconnect("Screen_size_changed", self, "_on_button_toggle")
  Event.disconnect("on_action_bound", self, "_on_key_bound")
  Event.disconnect("tab_changed", self, "_on_tab_changed")
  Event.disconnect("focus_changed", self, "_on_focus_changed")
  Event.disconnect("menu_box_rotated", self, "_on_menu_box_rotated")
  Event.disconnect("keyboard_action_biding", self, "_on_keyboard_action_biding")
  Event.disconnect("player_explode", self, "_on_player_explode")
  Event.disconnect("pause_menu_enter", self, "_on_pause_menu_enter")
  Event.disconnect("pause_menu_exit", self, "_on_pause_menu_exit")
  Event.disconnect("player_fall", self, "_on_player_falling")
  Event.disconnect("tetris_lines_removed", self, "_on_tetris_lines_removed")

func _enter_tree():
  connect_signals()
    
func _exit_tree():
  disconnect_signals()

func _on_player_jumped(): _sfx_play("jump")
func _on_player_rotate(dir): _sfx_play("rotateLeft" if dir == -1 else "rotateRight")
func _on_player_land(): _sfx_play("land")
func _on_menu_pressed(): _sfx_play("menuSelect")
func _on_gem_collected(_color, _position, _x): _sfx_play("gemCollect")
func _on_button_toggle(_value):  _sfx_play("menuValueChange")
func _on_key_bound(_value, _value2):  _sfx_play("menuValueChange")
func _on_tab_changed():  _sfx_play("menuFocus")
func _on_focus_changed(): _sfx_play("menuFocus")
func _on_menu_box_rotated(): _sfx_play("rotateRight")
func _on_keyboard_action_biding(): _sfx_play("menuValueChange")
func _on_player_explode(): _sfx_play("playerExplode")
func _on_pause_menu_enter(): _sfx_play("menuSelect")
func _on_pause_menu_exit(): _sfx_play("menuSelect")
func _on_player_falling(): _sfx_play("playerFalling")
func _on_tetris_lines_removed(): _sfx_play("tetrisLine")

func emit_play_sfx(sfx_name):
  emit_signal("play_sfx", sfx_name)
