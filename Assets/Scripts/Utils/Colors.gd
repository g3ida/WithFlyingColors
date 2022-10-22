class_name ColorUtils

const COLORS = ['blue', 'pink', 'yellow', 'purple']
const DARK_GREY = Color( 0.4, 0.4, 0.4, 1 )

static func get_group_color_index(group_name):
  if group_name == 'blue':
    return 0
  elif group_name == 'pink':
    return 1
  elif group_name == 'yellow':
    return 3
  elif group_name == 'purple':
    return 2
  push_error("unkown group" + group_name)
  return 0

static func get_basic_color(color_index: int):
  return get_skin_basic_color(_get_current_skin(), color_index)

static func get_skin_basic_color(skin, color_index: int):
  return get_skin_color(skin, String(color_index) + "-basic")

static func get_light_color(color_index: int):
  return get_color(String(color_index) + "-light")

static func get_dark_color(color_index: int):
  return get_color(String(color_index) + "-dark")

static func get_color(color_name: String):
  return get_skin_color(_get_current_skin(), color_name)

static func _get_current_skin():
  if Engine.editor_hint:
    return SkinLoader.DEFAULT_SKIN
  else:
    return Global.selected_skin

static func get_skin_color(skin, color_name: String):
  #ie: 0-basic, 1-dark2, 2-light, 3-background
  var color_component = color_name.split("-")
  if color_component.size() != 2:
    push_error("wrong requested color name: " + color_name)
    return null
  var index = color_component[0].to_int()
  var intensity = color_component[1]
  if not (intensity in SkinLoader.KEYS):
    push_error("wrong color intensity: " + intensity)
    intensity = "basic"
  return Color(skin[intensity][index])

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
