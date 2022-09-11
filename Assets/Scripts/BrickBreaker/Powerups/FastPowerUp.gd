extends PowerUpScript

func _enter_tree():
  set_process(false)
  var player = Global.player
  player.speed_limit = 2.0 * player.SPEED
  player.speed_unit = 2.0 * player.SPEED_UNIT
  
func _exit_tree():
  if is_still_relevant():
    var player = Global.player
    player.speed_limit = player.SPEED
    player.speed_unit = player.SPEED_UNIT

func _ready():
  pass

func is_still_relevant():
  var player = Global.player
  return abs(player.speed_limit - 2.0 * player.SPEED) < Constants.EPSILON
