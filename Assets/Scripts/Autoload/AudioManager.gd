extends Node

signal play_sfx(sfx_name)

onready var jump_sfx: AudioStreamPlayer = AudioStreamPlayer.new()
onready var rotate_left_sfx: AudioStreamPlayer = AudioStreamPlayer.new()
onready var rotate_right_sfx: AudioStreamPlayer = AudioStreamPlayer.new()

var x = preload("res://Assets/sfx/menu_select.ogg")

var sfx_data: Dictionary = {
  "jump": "res://Assets/sfx/jumping.ogg",
  "rotateLeft": "res://Assets/sfx/rotate.ogg",
  "rotateRight": "res://Assets/sfx/rotate.ogg",
  "menuSelect": "res://Assets/sfx/menu_select.ogg",
  "menuMove": "res://Assets/sfx/menu_move.ogg",
  "land": "res://Assets/sfx/fall.ogg",

  "checkpointHit": "res://Assets/sfx/menu_select.ogg",
  "checkpointLoad": "res://Assets/sfx/menu_select.ogg"
}

var sfx_pool: Dictionary = {}

func fill_sfx_pool():
  for key in sfx_data.keys():
    #fixme check for nulls
    var stream = load(sfx_data[key])
    var audio_player := AudioStreamPlayer.new()
    audio_player.stream = stream
    audio_player.stream.set_loop(false)
    sfx_pool[key] = audio_player
    add_child(audio_player)

func _ready():
  fill_sfx_pool()

func _sfx_play(sfx):
  # fixme: add safe access
  sfx_pool[sfx].play()

func connect_signals():
  self.connect("play_sfx", self, "_sfx_play")
  Event.connect("player_jumped", self, "_on_player_jumped")
  Event.connect("player_rotate", self, "_on_player_rotate")
  Event.connect("player_land", self, "_on_player_land")
  Event.connect("Play_button_pressed", self, "_on_menu_pressed")
  Event.connect("Stats_button_pressed", self, "_on_menu_pressed")
  Event.connect("Settings_button_pressed", self, "_on_menu_pressed")
  Event.connect("Quit_button_pressed", self, "_on_menu_pressed")
  Event.connect("Go_to_main_menu_pressed", self, "_on_menu_pressed")


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

func _enter_tree():
  connect_signals()
    
func _exit_tree():
  disconnect_signals()

func _on_player_jumped(): _sfx_play("jump")
func _on_player_rotate(dir): _sfx_play("rotateLeft" if dir == -1 else "rotateRight")
func _on_player_land(): _sfx_play("land")
func _on_menu_pressed(): _sfx_play("menuSelect")
