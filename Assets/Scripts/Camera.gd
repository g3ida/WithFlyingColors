extends Camera2D

func _ready():
	if is_current():
		Global.camera = self

func _process(delta):
	if is_current():
		Global.camera = self
