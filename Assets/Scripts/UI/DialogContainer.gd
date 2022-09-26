extends Control

export(NodePath) var DialogNode
onready var ColorRectNode = $ColorRect
onready var GameMenuNode = get_parent()

func _ready():
  var __ = DialogNode.connect("hide", "_hide_dialog")
  
func _exit_tree():
  DialogNode.disconnect("hide", "_hide_dialog")
  
func _show_dialog():
    DialogNode.show()
    ColorRectNode.show()
    GameMenuNode.handle_back_event = false
    
func _hide_dialog():
  if DialogNode != null:
    DialogNode.hide()
    ColorRectNode.hide()
    GameMenuNode.handle_back_event = true
  