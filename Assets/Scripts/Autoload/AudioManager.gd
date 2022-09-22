extends Node

signal play_sfx(sfx_name)

const MusicTrackManager = preload("res://Assets/Scenes/Audio/MusicTrackManager.tscn")

onready var jump_sfx: AudioStreamPlayer = AudioStreamPlayer.new()
onready var rotate_left_sfx: AudioStreamPlayer = AudioStreamPlayer.new()
onready var rotate_right_sfx: AudioStreamPlayer = AudioStreamPlayer.new()

# please keep alphabetic order for convenience
var sfx_data: Dictionary = {
  "brick": {
    "path": "res://Assets/sfx/brick.ogg",
    "volume": -4,
    "bus": "sfx"
  },
  "bricksSlide": {
    "path": "res://Assets/sfx/bricks_slide.ogg",
    "volume": 0,
    "bus": "sfx"
  },
  "gemCollect": {
    "path": "res://Assets/sfx/gem.ogg",
    "volume": -15,
    "bus": "sfx"
  },
  "jump": {
    "path": "res://Assets/sfx/jumping.ogg",
    "volume": -5,
    "bus": "sfx"
  },
  "land": {
    "path": "res://Assets/sfx/stand.ogg",
    "volume": -8,
    "bus": "sfx",
  },
  "menuFocus": {
    "path": "res://Assets/sfx/click2.ogg",
    "bus": "sfx"
  },
  "menuMove": {
    "path": "res://Assets/sfx/menu_move.ogg",
    "volume": 0,
    "bus": "sfx"
  },
  "menuSelect":{
    "path": "res://Assets/sfx/menu_select.ogg",
    "volume": 0,
    "bus": "sfx"
  },
  "menuValueChange": {
    "path": "res://Assets/sfx/click.ogg",
    "volume": 0,
    "bus": "sfx"
  },
  "pickup": {
    "path": "res://Assets/sfx/pickup.ogg",
    "volume": -4,
    "bus": "sfx"
  },
  "playerExplode": {
    "path": "res://Assets/sfx/die.ogg",
    "volume": -10,
    "bus": "sfx"
  },
  "playerFalling": {
    "path": "res://Assets/sfx/falling.ogg",
    "volume": -10
  },
  "rotateLeft": {
    "path": "res://Assets/sfx/rotatex.ogg",
    "volume": -20,
    "pitch_scale": 0.9,
    "bus": "sfx"
  },
  "rotateRight": {
    "path": "res://Assets/sfx/rotatex.ogg",
    "volume":  -20,
    "bus": "sfx"
  },
  "tetrisLine": {
    "path": "res://Assets/sfx/tetris_line.ogg",
    "volume": -7,
    "bus": "sfx"
  },
  "winMiniGame": {
    "path": "res://Assets/sfx/win_mini_game.ogg",
    "volume": 1,
    "bus": "sfx"
  },
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

    var bus = "sfx"
    if sfx_data[key].has("bus"):
      bus = sfx_data[key]["bus"]
 
    audio_player.bus = bus
    sfx_pool[key] = audio_player
    add_child(audio_player)
    audio_player.set_owner(self)

func stop_all_sfx():
  for sfx in sfx_pool:
    sfx_pool[sfx].stop()
    
func stop_all_except(sfx_list: Array):
  for sfx in sfx_pool:
    if not (sfx in sfx_list):
      sfx_pool[sfx].stop()

func _ready():
  pause_mode = PAUSE_MODE_PROCESS # se we can play sfx in pause menu
  set_process(false)
  music_track_manager = MusicTrackManager.instance()
  add_child(music_track_manager)
  music_track_manager.set_owner(self)
  fill_sfx_pool()

func _sfx_play(sfx):
  # fixme: add safe access
  sfx_pool[sfx].play()

func connect_signals():
  var __ = self.connect("play_sfx", self, "_sfx_play")
  __ = Event.connect("player_jumped", self, "_on_player_jumped")
  __ = Event.connect("player_rotate", self, "_on_player_rotate")
  __ = Event.connect("player_land", self, "_on_player_land")
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
  __ = Event.connect("picked_powerup", self, "_on_picked_powerup")
  __ = Event.connect("brick_broken", self, "_on_brick_broken")
  __ = Event.connect("break_breaker_win", self, "_on_win_mini_game")
  __ = Event.connect("brick_breaker_start", self, "_on_brick_breaker_start")
  __ = Event.connect("menu_button_pressed", self, "_on_menu_button_pressed")


func disconnect_signals():
  self.disconnect("play_sfx", self, "_sfx_play")
  Event.disconnect("player_jumped", self, "_on_player_jumped")
  Event.disconnect("player_rotate", self, "_on_player_rotate")
  Event.disconnect("player_land", self, "_on_player_land")
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
  Event.disconnect("picked_powerup", self, "_on_picked_powerup")
  Event.disconnect("brick_broken", self, "_on_brick_broken")
  Event.disconnect("break_breaker_win", self, "_on_win_mini_game")
  Event.disconnect("brick_breaker_start", self, "_on_brick_breaker_start")
  Event.disconnect("menu_button_pressed", self, "_on_menu_button_pressed")

func _enter_tree():
  connect_signals()
    
func _exit_tree():
  disconnect_signals()

func _on_player_jumped(): _sfx_play("jump")
func _on_player_rotate(dir): _sfx_play("rotateLeft" if dir == -1 else "rotateRight")
func _on_player_land(): _sfx_play("land")
func _on_menu_button_pressed(_menu_button): _sfx_play("menuSelect")
func _on_gem_collected(_color, _position, _x): _sfx_play("gemCollect")
func _on_button_toggle(_value):  _sfx_play("menuValueChange")
func _on_key_bound(_value, _value2):  _sfx_play("menuValueChange")
func _on_tab_changed():  _sfx_play("menuFocus")
func _on_focus_changed(): _sfx_play("menuFocus")
func _on_menu_box_rotated(): _sfx_play("rotateRight")
func _on_keyboard_action_biding(): _sfx_play("menuValueChange")
func _on_player_explode(): _sfx_play("playerExplode")
func _on_player_falling(): _sfx_play("playerFalling")
func _on_tetris_lines_removed(): _sfx_play("tetrisLine")
func _on_picked_powerup(): _sfx_play("pickup")
func _on_brick_broken(_color, _position): _sfx_play("brick")
func _on_win_mini_game(): _sfx_play("winMiniGame")
func _on_brick_breaker_start(): _sfx_play("bricksSlide")

func _on_pause_menu_enter():
  _sfx_play("menuSelect")
  music_track_manager.set_pause_menu_effect(true)
func _on_pause_menu_exit():
  _sfx_play("menuSelect")
  music_track_manager.set_pause_menu_effect(false)

func emit_play_sfx(sfx_name):
  emit_signal("play_sfx", sfx_name)
