class_name ColorUtils

const COLORS = ['blue', 'pink', 'yellow', 'purple']

static func get_color(color_name: String):
  if color_name == 'blue':
    return Color( 0, 0.9215, 1, 1 )
  elif color_name == 'pink':
    return Color( 1, 0, 0.5215, 1 )
  elif color_name == 'yellow':
    return Color( 0.8, 1, 0, 1 )
  elif color_name == 'purple':
    return Color( 0.635, 0.035, 0.964, 1 )
  else:
    push_error("unknown color group : " + color_name)
