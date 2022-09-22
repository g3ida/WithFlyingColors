tool
extends Control

const SubMenuScene = preload("res://Assets/Scenes/MainMenu/SubMenu.tscn")

onready var SubMenuNode = null

var buttons_def = [
  {"id": 0, "text": "New Game", "button": MenuButtons.NEW_GAME},
  {"id": 1, "text": "Continue", "button": MenuButtons.CONTINUE_GAME},
  {"id": 2, "text": "Load Game", "button": MenuButtons.LOAD_GAME},
]

func _ready():
  var blue = ColorUtils.get_color("blue")
  SubMenuNode = SubMenuScene.instance()
  SubMenuNode.color = ColorUtils.darken_rgb(blue, 0.0)
  SubMenuNode.top_color = ColorUtils.darken_rgb(blue, 0.115)
  
  SubMenuNode.buttons = []
  SubMenuNode.buttons_events = []
  SubMenuNode.buttons_ids = []
  for btn in buttons_def:
    SubMenuNode.buttons.append(btn["text"])
    SubMenuNode.buttons_events.append(btn["button"])
    SubMenuNode.buttons_ids.append(btn["id"])
  add_child(SubMenuNode)
  SubMenuNode.set_owner(self)
