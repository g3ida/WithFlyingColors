extends PowerUpScript

var is_done := false

func _ready():
  set_process(false)
  var bouncingBalls = []
  for c in BrickBreakerNode.BallsContainer.get_children():
    if c is BouncingBall:
      bouncingBalls.append(c)
  for b in bouncingBalls:
    for i in range(2):
      var ball = BrickBreakerNode.spawn_ball(b.color_group)
      ball.position = b.position
      var spawn_velocity = b.velocity.rotated(((i-0.5)*2)* Constants.PI3)
      if spawn_velocity.y > 0:
        spawn_velocity.y = -spawn_velocity.y
      ball.call_deferred("set_velocity", spawn_velocity)
  is_done = true

func is_still_relevant():
  return !is_done
