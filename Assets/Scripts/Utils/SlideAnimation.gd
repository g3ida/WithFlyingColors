var this: Node2D
var destination: Vector2
var speed_x: float
var speed_y: float
var notified = false
var animation_name: String

func _init(animation_name: String, this: Node2D, destination: Vector2, speed_x: float):
	self.this = this
	self.destination = destination
	self.animation_name = animation_name

	var diff_x = destination.x - this.position.x
	var diff_y = destination.y - this.position.y
	
	if diff_x != 0:
		self.speed_y = (diff_y / diff_x) * speed_x
	else:
		self.speed_y = self.speed_x
		
	self.speed_x = -speed_x if this.position.x > destination.x else speed_x
	self.speed_y = -abs(speed_y) if this.position.y > destination.y else abs(speed_y)

func update(delta):
	self.this.position.x += speed_x * delta
	self.this.position.y += speed_y * delta
	
	if (speed_x > 0 && this.position.x > destination.x) || (speed_x < 0 && this.position.x < destination.x) :
		this.position.x = destination.x
	if (speed_y > 0 && this.position.y > destination.y) || (speed_y < 0 && this.position.y < destination.y) :
		this.position.y = destination.y
		
	if (not notified) && this.position.x == destination.x && this.position.y == destination.y:
		notified = true
		notify()
		
func notify():
	Event.emit_signal("slide_animation_ended", animation_name)
