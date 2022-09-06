extends Sprite

onready var TweenNode = $Tween

func _ready():
  TweenNode.interpolate_property(self, "modulate:a", 1.0, 0.0, 0.2, 3, 1)
  TweenNode.start()
