extends Control

signal pressed()

var texture: ImageTexture = null setget set_texture, get_texture
var timestamp: int = -1 setget set_timestamp, get_timestamp
var description: String = "" setget set_description, get_description
var bg_color: Color = Color.white setget set_bg_color, get_bg_color
var has_focus: bool setget set_has_focus, get_has_focus

onready var TextureButtonNode = $Center/ColorRect/MarginContainer/VBoxContainer/TextureContainer/TextureButton 
onready var DescriptionNode = $Center/ColorRect/MarginContainer/VBoxContainer/DescriptionContainer/Description
onready var TimestampNode = $Center/ColorRect/MarginContainer/VBoxContainer/TimestampContainer/Timestamp
onready var ColorRectNode = $Center/ColorRect
onready var ContainerNode = $Center/ColorRect/MarginContainer/VBoxContainer
onready var MarginContainerNode = $Center/ColorRect/MarginContainer
onready var TextureBackgroundNode = $Center/ColorRect/MarginContainer/VBoxContainer/TextureContainer/TextureBackground
onready var BottomLine = $Center/ColorRect/MarginContainer/VBoxContainer/BottomLine
onready var ButtonNode = $Button

func _ready():
  _set_rect_size()
  set_texture(texture)
  set_timestamp(timestamp)
  set_description(description)
  set_bg_color(bg_color)

func set_texture(value):
  texture = value
  if TextureButtonNode != null and texture != null:
    TextureButtonNode.texture_normal = texture
    TextureButtonNode.texture_pressed = texture
    TextureButtonNode.texture_hover = texture
  
func get_texture():
  return texture

func set_description(value):
  description = value
  DescriptionNode.text = description

func get_description():
  return description

func set_timestamp(value):
  timestamp = value
  if timestamp == -1:
    TimestampNode.text = "----/--/-- --:--"
  else:
    var time = Time.get_datetime_dict_from_unix_time(value)
    var display_string : String = "%d/%02d/%02d %02d:%02d" % [time.year, time.month, time.day, time.hour, time.minute];
    TimestampNode.text = display_string

func get_timestamp():
  return timestamp

func set_bg_color(value):
  bg_color = value
  ColorRectNode.color = value
  BottomLine.color = ColorUtils.darken_rgb(value, 0.2)

func get_bg_color():
  return bg_color
  
func _set_rect_size():
  var sz = TextureButtonNode.texture_normal.get_size()
  TextureButtonNode.rect_min_size = sz
  TextureButtonNode.rect_size = sz
  self.rect_min_size = sz
  self.rect_size = sz
  MarginContainerNode.rect_size = ContainerNode.rect_size
  MarginContainerNode.rect_min_size = ContainerNode.rect_size

  var h_margin = 3
  MarginContainerNode.add_constant_override("margin_top", h_margin)
  MarginContainerNode.add_constant_override("margin_left", h_margin)
  MarginContainerNode.add_constant_override("margin_bottom", 0)
  MarginContainerNode.add_constant_override("margin_right", h_margin)
  
  TextureBackgroundNode.rect_min_size = TextureButtonNode.rect_min_size + 3*Vector2(h_margin, h_margin)
  TextureBackgroundNode.rect_size = TextureBackgroundNode.rect_min_size
  
  ColorRectNode.rect_size = MarginContainerNode.rect_size
  ColorRectNode.rect_min_size = MarginContainerNode.rect_size
  
  ButtonNode.rect_min_size = ColorRectNode.rect_size
  ButtonNode.rect_size = ColorRectNode.rect_size

func _on_Button_pressed():
  emit_signal("pressed")
  
func _process(_delta):
  if get_has_focus():
    ColorRectNode.rect_scale = Vector2(1.05, 1.05)
  else:
    ColorRectNode.rect_scale = Vector2(1, 1)
    
func _on_Button_mouse_entered():
  ButtonNode.grab_focus()

func set_has_focus(value):
  if value:
    ButtonNode.grabFocus()
  else:
    pass

func get_has_focus():
  return ButtonNode.has_focus()