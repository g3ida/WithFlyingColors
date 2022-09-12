class_name HSLColor

var h: float
var s: float
var l: float

func make_darker(l_shift_percentage: float) -> HSLColor:
  l =  clamp(l + l * l_shift_percentage, 0, 255.0)
  return self

func to_rgb() -> Color:
  var d = s * (1 - abs(2*l-1))
  var m = (l - 0.5*d)
  var x = d * (1- abs(fmod(h/60.0, 2)-1))
  var col = Color()
  if h >= 0 and h < 60:
    col.r = d+m
    col.g = x+m
    col.b = m
  elif h >= 60 and h < 120:
    col.r = x+m
    col.g = d+m
    col.b = m
  elif h >= 120 and h < 180:
    col.r = m
    col.g = d+m
    col.b = x+m
  elif h >= 180 and h < 240:
    col.r = m
    col.g = x+m
    col.b = d+m
  elif h >= 240 and h < 300:
    col.r = x+m
    col.g = m
    col.b = d+m
  else: # h >= 300 and h <= 360
    col.r = d+m
    col.g = m
    col.b = x+m
  return col
  
func _init(_h: float, _s: float, _l: float):
  h = _h; s = _s; l = _l
