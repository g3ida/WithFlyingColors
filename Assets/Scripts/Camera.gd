extends Camera2D

onready var tweenNode = $Tween

func _ready():
  if is_current():
    Global.camera = self

func _process(_delta):
  if is_current():
    Global.camera = self
    
func zoom_by(factor: float):
  tweenNode.interpolate_property(self, "zoom", zoom, Vector2(factor, factor), 1.0)
  tweenNode.start()
  zoom = Vector2(factor, factor)
