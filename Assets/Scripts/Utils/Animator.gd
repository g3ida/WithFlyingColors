class_name Animator

const Interpolation = preload("res://Assets/Scripts/Utils/Interpolation/Interpolation.gd")

var start: float
var end: float
var update_func
var interpolation: Interpolation
var duration: float
var timer = 0.0
var delay = 0.0
var reversed = false

func _init(_start: float, _end: float, _update_func, _duration: float, _delay: float, _interpolation: Interpolation):
  self.start = _start
  self.end = _end
  self.update_func = _update_func
  self.interpolation = _interpolation
  self.duration = _duration
  self.timer = 0.0
  self.delay = _delay

func reset():
  self.timer = 0

func reverse():
  self.reversed = true

func is_reversed() -> bool:
  return self.reversed

func is_running() -> bool:
  return self.timer < self.duration + self.delay

func update(delta: float):
  var time = max(0, self.timer - self.delay)

  var fraction = time/self.duration
  if (self.reversed):
    fraction = 1 - fraction

  var i = interpolation.apply(self.start, self.end, fraction)
  update_func.call_func(i)

  self.timer += delta
  if self.timer >= self.duration + self.delay:
    self.timer = duration + self.delay
  
