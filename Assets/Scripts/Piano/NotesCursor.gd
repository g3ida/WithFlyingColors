extends Sprite

const SPEED = 10 * Global.WORLD_TO_SCREEN

onready var AnimationPlayerNode = $AnimationPlayer
onready var TweenNode = $Tween

func _ready():
  $AnimationPlayer.play("Blink")

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
