extends "Interpolation.gd"

func _init(value: float, power: float, bounces: int, scale: float).(value, power, bounces, scale):
	pass
	
func _apply(a: float):
	if a > 0.99:
		return 1
	return pow(value, power * (a - 1)) * sin(a * bounces) * scale
