extends CenterContainer

signal slot_pressed(id)

export var disable_empty_slots = false
export var centered_on_screen = true

onready var SaveSlot1Node = $HBoxContainer/SaveSlot1
onready var SaveSlot2Node = $HBoxContainer/SaveSlot2
onready var SaveSlot3Node = $HBoxContainer/SaveSlot3
onready var save_slots = [SaveSlot1Node, SaveSlot2Node, SaveSlot3Node]

var slots_colors = Constants.COLOR_GROUPS

var currently_selected_slot = null

func _ready():
  set_process(false)
  for i in range(save_slots.size()):
    save_slots[i].texture = SaveGame.load_slot_image(i)
    var meta_data = SaveGame.get_slot_meta_data(i)
    if meta_data != null:
      save_slots[i].timestamp = meta_data["save_time"]
    else:
      save_slots[i].description = "<EMPTY>"
      if disable_empty_slots:
        save_slots[i].is_disabled = true
    var color_index = ColorUtils.get_group_color_index(slots_colors[i])
    var color = ColorUtils.get_basic_color(color_index)
    save_slots[i].bg_color = color
    save_slots[i].id = i

  for i in range(save_slots.size()):
    if not save_slots[i].is_disabled:
      save_slots[i].has_focus = true
      break

  if centered_on_screen:
    rect_position = (get_viewport_rect().size - rect_size) * 0.5
    
func _process(_delta):
  pass #set_process = false

func _on_SaveSlot1_pressed():
  emit_signal("slot_pressed", 0)

func _on_SaveSlot2_pressed():
  emit_signal("slot_pressed", 1)

func _on_SaveSlot3_pressed():
  emit_signal("slot_pressed", 2)
