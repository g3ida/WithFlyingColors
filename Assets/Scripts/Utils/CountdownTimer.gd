var duration: float
var timer
	
func _init(duration: float, isSet: bool):
	self.duration = duration
	timer = duration if isSet else 0
	
func reset():
	timer = duration
		
func is_running():
	return timer > 0
	
func step(delta: float):
	timer = max(timer - delta, 0)
	
func stop():
	timer = 0
