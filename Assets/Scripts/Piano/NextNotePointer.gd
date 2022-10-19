extends Node2D

const SPEED = 20 * Global.WORLD_TO_SCREEN
onready var TweenNode = $Tween
onready var AnimationPlayerNode = $ColorRect/AnimationPlayer

func _ready():
  AnimationPlayerNode.play("Blink")

func move_to_position(dest_pos: Vector2):
  var duration = (position - dest_pos).length() / SPEED
  TweenNode.remove_all()
  TweenNode.interpolate_property(
    self,
    "position",
    self.position,
    dest_pos,
    duration,
    Tween.TRANS_QUAD,
    Tween.EASE_IN_OUT)
  TweenNode.start()
