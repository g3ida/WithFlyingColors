extends Node2D

onready var PianoNodeScene = $Piano

func _ready():
  pass

func _on_TriggerArea_body_entered(body):
  if body != Global.player:
    return
  if PianoNodeScene.is_stopped():
    PianoNodeScene._start_game()
