extends Sprite

func set_color(color_name: String):
  self.modulate = ColorUtils.get_color(color_name)
  
func _ready():
  pass
