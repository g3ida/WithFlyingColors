extends Sprite

func _ready():
  var color_index = ColorUtils.get_group_color_index(get_parent().color_group)
  var color = ColorUtils.get_basic_color(color_index)
  self.modulate = ColorUtils.darken_rgb(color, 0.4)
