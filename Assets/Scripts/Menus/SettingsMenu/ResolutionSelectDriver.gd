extends UISelectDriver

var resolutions = []

func _init().():
  var vals = [
    Vector2(1920, 1080),
    Vector2(1280, 720),
    Vector2(1024, 576),
    Vector2(800, 450)]
  
  var screen_size = OS.get_screen_size()
  for el in vals:
    if el.x <= screen_size.x && el.y <= screen_size.y:
      self.items.append(String(el.x) + "x" + String(el.y))
      self.item_values.append(el)
      resolutions.append(el)


func on_item_selected(_item: String):
	pass
	
func get_default_selected_index() -> int:
  var w_size = Settings.window_size
  for i in range(0, resolutions.size()):
    if resolutions[i] == w_size:
      return i
  return 0

func _ready():
	pass
