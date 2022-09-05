extends Node2D

onready var DeathZoneNode = $DeathZone
onready var BouncingBallNode = $BouncingBall
var is_playing = false

func _ready():
  BouncingBallNode.death_zone = DeathZoneNode

func connect_signals():
  var __ = Event.connect("checkpoint_reached", self, "_on_checkpoint_hit")
  __ = Event.connect("checkpoint_loaded", self, "reset")
  
func disconnect_signals():
  Event.disconnect("checkpoint_reached", self, "_on_checkpoint_hit")
  Event.disconnect("checkpoint_loaded", self, "reset")
      
func _enter_tree():
  connect_signals()

func _exit_tree():
  disconnect_signals()
  
func reset():
  if (is_playing):
    BouncingBallNode.reset()
  
func _on_checkpoint_hit():
  pass
