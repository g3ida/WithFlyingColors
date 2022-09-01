extends Area2D

signal playerEntred()

func _ready():
  pass

func _on_CheckpointArea_body_entered(_body):
  emit_signal("playerEntred")
