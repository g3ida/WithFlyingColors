extends Node2D

const EPSILON: float = 0.0001
const WORLD_TO_SCREEN = 100

var camera: Camera2D = null
var player: KinematicBody2D = null

var _player_sprite
var selected_skin = SkinLoader.GOOGL_SKIN setget set_selected_skin, get_selected_skin

enum EntityType {PLATFORM, FALLZONE, LAZER, BULLET, BALL, BRICK_BREAKER}

func _ready():
  set_process(false)

#the opposite is physical checkoint by using checkpointArea.tscn
func trigger_functional_checkoint():
  var checkpoint = CheckpointArea.new()
  checkpoint.color_group = "blue"
  checkpoint._on_CheckpointArea_body_entered(player)

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