extends Control


onready var DescriptionNode = $Description
onready var ButtonNode = $Button

var levelName: String setget set_level_name, get_level_name
var levelScene: String setget set_level_scene, get_level_scene

func _ready():
  pass
  
func set_level_name(name: String):
  levelName = name
  DescriptionNode.text = name
  
func get_level_name() -> String:
  return levelName

func set_level_scene(level_scene: String):
  levelScene = level_scene

func get_level_scene() -> String:
  return levelScene

func _on_Button_pressed():
  Event.emit_menu_button_pressed(MenuButtons.SELECT_LEVEL)
  get_parent().get_parent().navigate_to_level_screen(levelScene)

func _on_Button_mouse_entered():
  ButtonNode.grab_focus()

func set_focus():
  ButtonNode.grab_focus()
