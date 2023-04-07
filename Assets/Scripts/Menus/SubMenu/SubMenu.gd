tool
extends Control

const SubMenuItemScene = preload("res://Assets/Scenes/MainMenu/SubMenuItem.tscn")

export(Array, String) var buttons = []
export(Array, int) var buttons_ids = []
export(Array, int) var buttons_events = []
export(Array, bool) var buttons_disabled = []

export(Color) var color
export(Color) var top_color

onready var ContainerNode = $VBoxContainer
onready var TopNode = $VBoxContainer/Top

var button_nodes = []

func _ready():
  set_process(false)
  assert(buttons.size() == buttons_ids.size())
  assert(buttons.size() == buttons_events.size())
  assert(buttons.size() == buttons_disabled.size())
  
  for i in buttons.size():
    var item = SubMenuItemScene.instance()
    item.color = color
    item.text = buttons[i]
    item.id = buttons_ids[i]
    item.event = buttons_events[i]
    item.disabled = buttons_disabled[i]
    ContainerNode.add_child(item)
    button_nodes.append(item)
    item.set_owner(ContainerNode)
    rect_size.y += item.rect_size.y
    rect_min_size.y += item.rect_min_size.y
  ContainerNode.margin_top = 0
  ContainerNode.margin_bottom = 0
  TopNode.modulate = top_color
  for b in button_nodes:
    if not b.disabled:
      b.button_grab_focus()
      break
      
func update_colors():
  for ch in ContainerNode.get_children():
    if ch is TextureRect:
      ch.modulate = top_color
    else:
      ch.color = color
      ch.update_colors()
