extends Sprite

func _ready():
  var color = ColorUtils.get_color(get_parent().color_group)
  self.modulate = ColorUtils.darken_rgb(color, 0.4)
