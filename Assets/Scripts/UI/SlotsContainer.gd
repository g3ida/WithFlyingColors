extends Control

signal slot_pressed(id, action)

export var centered_on_screen_v = false
export var centered_on_screen_h = false

onready var BoxContainerNode = $HBoxContainer
onready var SaveSlot1Node = $HBoxContainer/SaveSlot1
onready var SaveSlot2Node = $HBoxContainer/SaveSlot2
onready var SaveSlot3Node = $HBoxContainer/SaveSlot3
onready var save_slots = [SaveSlot1Node, SaveSlot2Node, SaveSlot3Node]

var slots_colors = Constants.COLOR_GROUPS

func _ready():
  set_process(false)
  self.rect_size = BoxContainerNode.rect_size
  for i in range(save_slots.size()):
    save_slots[i].texture = SaveGame.load_slot_image(i)
    save_slots[i].id = i
    save_slots[i].update_meta_data()
    
  for i in range(save_slots.size()):
    if not save_slots[i].is_disabled:
      save_slots[i].has_focus = true
      break

  if centered_on_screen_h:
    rect_position.x = (get_viewport_rect().size.x - rect_size.x) * 0.5
  if centered_on_screen_v:
    rect_position.y = (get_viewport_rect().size.y - rect_size.y) * 0.5
  
  save_slots[SaveGame.current_slot_index].set_has_focus(true)
    
func _process(_delta):
  pass #set_process = false

func _on_SaveSlot1_pressed(action):
  emit_signal("slot_pressed", 0, action)

func _on_SaveSlot2_pressed(action):
  emit_signal("slot_pressed", 1, action)

func _on_SaveSlot3_pressed(action):
  emit_signal("slot_pressed", 2, action)

func update_slots():
  for i in range(save_slots.size()):
    save_slots[i].update_meta_data()

func update_slot(id: int, set_focus: bool = false):
  save_slots[id].update_meta_data()
  if (set_focus):
    save_slots[id].hide_action_buttons()

# which slot is selected for save game
func set_game_current_selected_slot(index):
  for i in range(save_slots.size()):
    if (index == i):
      save_slots[i].update_meta_data()
      save_slots[i].set_border(true)
    else:
      save_slots[i].set_border(false)
