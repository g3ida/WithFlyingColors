class_name ColorUtils

const COLORS = ['blue', 'pink', 'yellow', 'purple']
const DARK_GREY = Color( 0.4, 0.4, 0.4, 1 )

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

# 204 255 0 => 72(/360) 100(/100) 50(/100)

#M = 255
#m = 0
# d= 1.0
# l= 255/2 / 255 => 0.5
# s = 1
# t = 

static func rgb_to_hsl(color: Color) -> HSLColor:
  var R = color.r*255.0; var G = color.g*255.0; var B = color.b*255.0
  var M = max(max(R, G), B)
  var m = min(min(R, G), B)
  var d = (M - m) / 255.0
  var L = (0.5*(M+m)) / 255.0
  var S = 0.0 if L <= 0.0 else d/(1-abs(2*L-1)+0.001)
  var t = acos((R- 0.5*G - 0.5*B) / sqrt((R*R + G*G + B*B - R*G - R*B - G*B)+0.001)) * Constants.RAD_TO_DEGREES
  var H = 360.0-t if B > G else t
  return HSLColor.new(H, S, L)

static func darken_rgb(color: Color, _l_shift_percentage: float) -> Color:
  return rgb_to_hsl(color).make_darker(-_l_shift_percentage).to_rgb()
