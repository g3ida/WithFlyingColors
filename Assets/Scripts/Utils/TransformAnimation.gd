const Interpolation = preload("Interpolation/Interpolation.gd")

var animation_duration: float
var interpolation: Interpolation
var offset_y: float

var timer: float = 0.0
var started: bool = false

func _init(_animation_duration: float, _interpolation: Interpolation, _offset_y: float):
  self.animation_duration = _animation_duration
  self.interpolation = _interpolation
  self.offset_y = _offset_y

func isDone() -> bool:
  return timer <= 0.0 and started

func isRunning() -> bool:
  return timer > 0.0

func start() -> void:
  if isRunning():
    return
  timer = animation_duration
  started = true
  
func step(node: Node2D, sprite: AnimatedSprite, delta: float):
  if timer > 0:
    var sin_rot = sin(node.rotation)
    var cos_rot = cos(node.rotation)
    var nomrlalized = timer / animation_duration
    var mean = 1.0
    var i = interpolation.apply(0.0, 1.0, nomrlalized) - mean
    
    sprite.scale.x = mean + (i * abs(cos_rot) - abs(sin_rot) * i)
    sprite.scale.y = mean + (i * abs(sin_rot) - abs(cos_rot) * i)
    
    timer -= delta
    sprite.offset.y = offset_y * (1.0 - sprite.scale.y) * cos_rot
    sprite.offset.x = offset_y * (1.0 - sprite.scale.x) * sin_rot
  else:
    reset(sprite)
    
func reset(sprite: AnimatedSprite):
  timer = 0
  started = false
  sprite.scale = Vector2(1.0, 1.0)
  sprite.offset = Vector2(0.0, 0.0)
