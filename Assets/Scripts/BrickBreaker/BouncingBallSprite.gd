extends Sprite

func set_color(color_group: String):
  var color_index = ColorUtils.get_group_color_index(color_group)
  var color = ColorUtils.get_basic_color(color_index)
  self.modulate = color
  
func _ready():
  pass
