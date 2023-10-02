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

func check_state():
  assert(buttons.size() == buttons_ids.size())
  assert(buttons.size() == buttons_events.size())
  assert(buttons.size() == buttons_disabled.size())

func _ready():
  set_process(false)
  check_state()
  init_items()
  init_container()
  set_focus_dependencies() # fixes the arrows(up/down) navigation bug when rotating the menu box beyond 360 degrees.
  set_focus_button()

func init_container():
  ContainerNode.margin_top = 0
  ContainerNode.margin_bottom = 0
  TopNode.modulate = top_color

func init_items():
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

func set_focus_button():
  for b in button_nodes:
    if not b.disabled:
      b.button_grab_focus()
      break

func set_focus_dependencies():
  var active_indexes = []
  for i in range(button_nodes.size()):
    if not button_nodes[i].disabled:
      active_indexes.append(i)
  for i in range(active_indexes.size()-1):
    var current = button_nodes[active_indexes[i]]
    var next = button_nodes[active_indexes[i+1]]
    current.ButtonNode.focus_next = next.ButtonNode.get_path()
    next.ButtonNode.focus_previous = current.ButtonNode.get_path()
    current.ButtonNode.focus_neighbour_bottom = next.ButtonNode.get_path()
    next.ButtonNode.focus_neighbour_top = current.ButtonNode.get_path()
      
func update_colors():
  for ch in ContainerNode.get_children():
    if ch is TextureRect:
      ch.modulate = top_color
    else:
      ch.color = color
      ch.update_colors()

