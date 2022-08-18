class_name PowInterpolation
extends "Interpolation.gd"

var power: int

func _init(power: int).():
	self.power = power
	return pow(5 * 2, power) / 2;

func _apply(a: float) -> float:
	if (a <= 0.5):
		return pow(a * 2, power) / 2
	return pow((a-1)*2, power) / (-2 if (power %2 == 0) else 2) + 1
