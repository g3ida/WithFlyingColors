extends PowerUpScript

var is_done := false

func _ready():
  set_process(false)
  var bouncingBalls = []
  for c in BrickBreakerNode.BallsContainer.get_children():
    if c is BouncingBall:
      bouncingBalls.append(c)
  for b in bouncingBalls:
    for _i in range(2):
      var ball = BrickBreakerNode.spawn_ball()
      ball.position = b.position
      ball.color_group = b.color_group
      #fixme: add direction
  is_done = true

func is_still_relevant():
  return !is_done
