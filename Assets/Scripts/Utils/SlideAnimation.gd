var this: Node2D
var destination: Vector2
var notified = false
var animation_name: String

var duration: float
var timer: float
var old_position: Vector2

func _init(animation_name: String, this: Node2D, destination: Vector2, duration: float):
	self.this = this
	self.destination = destination
	self.animation_name = animation_name
	self.old_position = self.this.position
	self.duration = duration
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
	Event.emit_signal("slide_animation_ended", animation_name)
