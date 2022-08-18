extends "ElasticInterpolation.gd"

func _init(value: float, power: float, bounces: int, scale: float).(value, power, bounces, scale):
	pass

func _apply(a: float):
	if a == 0:
		return 0
	a = 1 - a
	return (1 - pow(value, power * (a - 1)) * sin(a * bounces) * scale);

