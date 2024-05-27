extends Node2D

var camera: Camera2D = null
var player: KinematicBody2D = null
var cutscene = null
var gem_hud = null
var pause_menu = null

var _player_sprite
var selected_skin = SkinLoader.DEFAULT_SKIN setget set_selected_skin, get_selected_skin

func _ready():
  set_process(false)

func get_player_sprite():
  if _player_sprite == null:
    _player_sprite = PlayerSpriteGenerator.get_texture()
  return _player_sprite

func set_selected_skin(skin):
  if selected_skin != skin:
    selected_skin = skin
    _player_sprite = PlayerSpriteGenerator.get_texture()

func get_selected_skin():
  return selected_skin
