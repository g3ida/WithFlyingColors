class_name PowerUpScript
extends Node2D

signal is_destroyed(this)

var BrickBreakerNode: Node2D = null 
var is_incremental = false # player can have multiple instances of this power up

func set_brick_breaker_node(brickNode: Node2D):
  BrickBreakerNode = brickNode

func emit_is_destroyed():
  emit_signal("is_destroyed")

func is_still_relevant() -> bool:
  return true