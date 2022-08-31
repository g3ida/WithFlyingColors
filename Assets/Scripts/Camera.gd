extends Camera2D

func _ready():
  if is_current():
    Global.camera = self
    zoom_by(1.5)

func _process(_delta):
  if is_current():
    Global.camera = self

func zoom_by(factor: float):
  zoom = Vector2(factor, factor)
