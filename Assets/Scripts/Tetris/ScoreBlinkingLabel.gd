extends Label

onready var animationPlayerNode = $AnimationPlayer

func set_value(value):
  text = value
  animationPlayerNode.play("Blink")
  
func _ready():
  pass
