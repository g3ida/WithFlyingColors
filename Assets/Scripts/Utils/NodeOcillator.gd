class_name NodeOcillator

var timer = 0
var inital_position: Vector2
var node: Node2D
var amplitude: float = 1.0
var duration = 1.0

func _init(_node: Node2D, _amplitude: float, _duration: float):
  self.node = _node
  self.amplitude = _amplitude
  self.duration = _duration
  self.inital_position = _node.position

func update(delta):
  self.timer += delta
  if self.timer > duration:
    self.timer = 0
  node.position.y = self.inital_position.y + self.amplitude * sin(2 * PI * timer / self.duration)
