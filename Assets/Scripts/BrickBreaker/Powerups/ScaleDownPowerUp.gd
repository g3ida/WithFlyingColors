extends PowerUpScript

const TWEEN_TIME = 0.7
const SCALE_FACTOR = 0.7

onready var TweenNode = $Tween
  
func _exit_tree():
  if is_still_relevant():
    var player = Global.player
    player.scale = Vector2(1.0, 1.0)
  
func interpolate_size(player, before, after, seconds):
    TweenNode.interpolate_property(player, "scale", Vector2(before,before), Vector2(after,after), seconds, Tween.TRANS_LINEAR, Tween.EASE_IN_OUT)
    TweenNode.start()

func _ready():
  set_process(false)
  var player = Global.player
  interpolate_size(player, player.scale.x, SCALE_FACTOR, TWEEN_TIME)

func is_still_relevant():
  var player = Global.player
  return abs(player.scale.x - SCALE_FACTOR) < Constants.EPSILON
