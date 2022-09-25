extends Node2D

const SPEED = 70 * Global.WORLD_TO_SCREEN

onready var TweenNode = $Tween
onready var destination_pos = position
onready var AnimationPlayerNode = $Sprite/AnimationPlayer

func _ready():
  AnimationPlayerNode.play("Bump")
  set_process(false)

func move_to(pos: Vector2):
  if destination_pos != pos:
    destination_pos = pos
    create_tween()

func _process(_delta):
    pass
    
func create_tween():
  TweenNode.remove_all()
  var distance = (destination_pos - position).length()
  var time = distance / SPEED
  TweenNode.interpolate_property(
    self,
    "position",
    position,
    destination_pos,
    time,
    Tween.TRANS_LINEAR,
    Tween.EASE_IN_OUT)
  TweenNode.start()
