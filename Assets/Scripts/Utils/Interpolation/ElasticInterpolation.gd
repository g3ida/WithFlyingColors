extends "Interpolation.gd"

var value: float
var power: float
var bounces: int
var scale : float

func _init(_value: float, _power: float, _bounces: int, _scale: float).():
  self.value = _value
  self.power = _power
  self.bounces = int(_bounces * PI * (1 if _bounces % 2 == 0 else -1))
  self.scale = _scale
  
func apply (start: float, end: float, a: float) -> float:
  return start + (end - start) * _apply(a);
  
func _apply(_a: float) -> float:
  return 0.0
