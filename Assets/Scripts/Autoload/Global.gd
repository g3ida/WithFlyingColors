extends Node2D

const EPSILON: float = 0.0001
const WORLD_TO_SCREEN = 100

var camera: Camera2D = null
var player: KinematicBody2D = null

enum EntityType {PLATFORM, FALLZONE, LAZER, BULLET, BALL, BRICK_BREAKER}

func _ready():
  set_process(false)

#the opposite is physical checkoint by using checkpointArea.tscn
func trigger_functional_checkoint():
  var checkpoint = CheckpointArea.new()
  checkpoint.color_group = "blue"
  checkpoint._on_CheckpointArea_body_entered(player)
