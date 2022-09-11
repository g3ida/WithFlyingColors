extends PowerUpScript

func _enter_tree():
  set_process(false)
  var player = Global.player
  player.speed_limit = 0.5 * player.SPEED
  player.speed_unit = 0.5 * player.SPEED_UNIT
  
func _exit_tree():
  if is_still_relevant():
    var player = Global.player
    player.speed_limit = player.SPEED
    player.speed_unit = player.SPEED_UNIT

func _ready():
  pass

func is_still_relevant():
  var player = Global.player
  return abs(player.speed_limit - 0.5 * player.SPEED) < Constants.EPSILON