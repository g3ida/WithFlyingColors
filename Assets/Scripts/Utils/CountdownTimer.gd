var duration: float
var timer
  
func _init(_duration: float, isSet: bool):
  self.duration = _duration
  timer = _duration if isSet else 0.0
  
func reset():
  timer = duration
    
func is_running():
  return timer > 0
  
func step(delta: float):
  timer = max(timer - delta, 0)
  
func stop():
  timer = 0
