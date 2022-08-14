var value: float
var power: float
var bounces: int
var scale : float

func _init(value: float, power: float, bounces: int, scale: float):
	self.value = value
	self.power = power
	self.bounces = bounces * PI * (1 if bounces % 2 == 0 else -1)
	self.scale = scale
	
func apply (start: float, end: float, a: float) -> float:
	return start + (end - start) * _apply(a);
	
func _apply(a: float) -> float:
	return 0.0
