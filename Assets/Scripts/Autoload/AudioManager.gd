extends Node

signal play_sfx(sfx_name)

const MusicTrackManager = preload("res://Assets/Scenes/Audio/MusicTrackManager.tscn")
const BASE_PATH = "res://Assets/sfx/"

onready var jump_sfx: AudioStreamPlayer = AudioStreamPlayer.new()
onready var rotate_left_sfx: AudioStreamPlayer = AudioStreamPlayer.new()
onready var rotate_right_sfx: AudioStreamPlayer = AudioStreamPlayer.new()

# please keep alphabetic order for convenience
var sfx_data: Dictionary = {
  "brick": {
    "path": BASE_PATH + "brick.ogg",
    "volume": -4,
  },
  "bricksSlide": {
    "path": BASE_PATH + "bricks_slide.ogg",
    "volume": 0,
  },
  "gemCollect": {
    "path": BASE_PATH + "gem.ogg",
    "volume": -15,
  },
  "jump": {
    "path": BASE_PATH + "jumping.ogg",
    "volume": -5,
  },
  "land": {
    "path": BASE_PATH + "stand.ogg",
    "volume": -8,
  },
  "menuFocus": {
    "path": BASE_PATH + "click2.ogg",
  },
  "menuMove": {
    "path": BASE_PATH + "menu_move.ogg",
    "volume": 0,
  },
  "menuSelect":{
    "path": BASE_PATH + "menu_select.ogg",
    "volume": 0,
  },
  "menuValueChange": {
    "path": BASE_PATH + "click.ogg",
    "volume": 0,
  },
  "pageFlip": {
    "path": BASE_PATH + "piano/page-flip.ogg",
    "volume": 5,
  },
  "piano_do": {
    "path": BASE_PATH + "piano/do.ogg",
    "volume": 1,
  },
  "piano_re": {
    "path": BASE_PATH + "piano/re.ogg",
    "volume": 1,
  },
  "piano_mi": {
    "path": BASE_PATH + "piano/mi.ogg",
    "volume": 1,
  },
  "piano_fa": {
    "path": BASE_PATH + "piano/fa.ogg",
    "volume": 1,
  },
  "piano_sol": {
    "path": BASE_PATH + "piano/sol.ogg",
    "volume": 1,
  },
  "piano_la": {
    "path": BASE_PATH + "piano/la.ogg",
    "volume": 1,
  },
  "piano_si": {
    "path": BASE_PATH + "piano/si.ogg",
    "volume": 1,
  },
  "pickup": {
    "path": BASE_PATH + "pickup.ogg",
    "volume": -4,
  },
  "playerExplode": {
    "path": BASE_PATH + "die.ogg",
    "volume": -10,
  },
  "playerFalling": {
    "path": BASE_PATH + "falling.ogg",
    "volume": -10
  },
  "rotateLeft": {
    "path": BASE_PATH + "rotatex.ogg",
    "volume": -20,
    "pitch_scale": 0.9,
  },
  "rotateRight": {
    "path": BASE_PATH + "rotatex.ogg",
    "volume":  -20,
  },
  "tetrisLine": {
    "path": BASE_PATH + "tetris_line.ogg",
    "volume": -7,
  },
  "winMiniGame": {
    "path": BASE_PATH + "win_mini_game.ogg",
    "volume": 1,
  },
  "wrongAnswer": {
    "path": BASE_PATH + "piano/wrong-answer.ogg",
    "volume": 10,
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
  __ = Event.connect("sfx_volume_changed", self, "_on_button_toggle")
  __ = Event.connect("music_volume_changed", self, "_on_button_toggle")
  __ = Event.connect("piano_note_pressed", self, "_on_piano_note_pressed")
  __ = Event.connect("piano_note_released", self, "_on_piano_note_released")
  __ = Event.connect("page_flipped", self, "_on_page_flipped")
  __ = Event.connect("wrong_piano_note_played", self, "_on_wrong_piano_note_played")

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
  Event.disconnect("sfx_volume_changed", self, "_on_button_toggle")
  Event.disconnect("music_volume_changed", self, "_on_button_toggle")
  Event.disconnect("piano_note_pressed", self, "_on_piano_note_pressed")
  Event.disconnect("piano_note_released", self, "_on_piano_note_released")
  Event.disconnect("page_flipped", self, "_on_page_flipped")
  Event.disconnect("wrong_piano_note_played", self, "_on_wrong_piano_note_played")

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
func _on_piano_note_pressed(note): _sfx_play("piano_" + note)
func _on_piano_note_released(_note): pass #nothing to do
func _on_page_flipped(): _sfx_play("pageFlip")
func _on_wrong_piano_note_played(): _sfx_play("wrongAnswer")

func _on_pause_menu_enter():
  _sfx_play("menuSelect")
  

func _on_pause_menu_exit():
  _sfx_play("menuSelect")

func emit_play_sfx(sfx_name):
  emit_signal("play_sfx", sfx_name)

func pause_all_sfx():
  for sfx in sfx_pool:
    var audio_player = (sfx_pool[sfx] as AudioStreamPlayer)
    if audio_player.playing:
      audio_player.stream_paused = true

func resume_all_sfx():
  for sfx in sfx_pool:
    var audio_player = (sfx_pool[sfx] as AudioStreamPlayer)
    if audio_player.playing:
      audio_player.stream_paused = false