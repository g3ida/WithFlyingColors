extends Button

func _ready():
  set_process(false)

func update_position_x(value):
  self.rect_position.x = value
