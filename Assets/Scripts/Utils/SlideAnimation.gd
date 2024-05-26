var this: Node2D
var destination: Vector2
var notified = false
var animation_name: String

var duration: float
var timer: float
var old_position: Vector2

func _init(_animation_name: String, _this: Node2D, _destination: Vector2, _duration: float):
  self.this = _this
  self.destination = _destination
  self.animation_name = _animation_name
  self.old_position = self.this.position
  self.duration = _duration
  self.timer = 0

func update(delta):
  timer += delta
  if timer > duration:
    timer = duration

  var weight = timer / duration
  self.this.position = self.this.position.linear_interpolate(self.destination, weight)

  if (not notified) && this.position.x == destination.x && this.position.y == destination.y:
    notified = true
    notify()
    
func notify():
  Event.emit_slide_animation_ended(animation_name)
