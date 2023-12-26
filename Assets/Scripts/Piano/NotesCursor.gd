extends Sprite

const SPEED = 10 * Global.WORLD_TO_SCREEN

onready var AnimationPlayerNode = $AnimationPlayer

var tweener: SceneTreeTween

func _ready():
  $AnimationPlayer.play("Blink")

func move_to_position(dest_pos: Vector2):
  var duration = (position - dest_pos).length() / SPEED
  if tweener:
    tweener.kill()
  tweener = create_tween()
  var __ = tweener.tween_property(
    self,
    "position",
    dest_pos,
    duration
  ).from(self.position
  ).set_trans(Tween.TRANS_QUAD
  ).set_ease(Tween.EASE_IN_OUT)
