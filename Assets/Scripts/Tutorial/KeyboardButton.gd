tool
extends Control

const MARGINS_X = 23
const BUTTON_WIDTH = 103

export var key_text = ""

onready var LabelNode = $Label
onready var ButtonTextureNode = $NinePatchRect
onready var ArrowSpriteNode = $Arrow

func _ready():
  var action_list = InputMap.get_action_list(key_text)
  if action_list != null and !action_list.empty():
    var key = InputUtils.get_first_key_keyboard_event_from_action_list(action_list)
    if key != null:
      var index = [KEY_RIGHT, KEY_DOWN, KEY_LEFT, KEY_UP].find(key.scancode)
      if index != -1:
        ArrowSpriteNode.visible = true
        ArrowSpriteNode.rotate(index * Constants.PI2)
        LabelNode.text = "aaa" #just to fill the necessary space 
        LabelNode.visible = false
      else:
        LabelNode.text = OS.get_scancode_string(key.scancode)
      _on_Label_resized()

func _on_Label_resized():
  var width = max($Label.rect_size.x + MARGINS_X, BUTTON_WIDTH)
  var height = $Label.rect_size.y
  
  $NinePatchRect.rect_size = Vector2(width, height)
  $NinePatchRect.rect_min_size = $NinePatchRect.rect_size
  self.rect_size = $NinePatchRect.rect_size
  self.rect_min_size = $NinePatchRect.rect_size
  $Label.rect_position.x = (width - $Label.rect_size.x) * 0.72
