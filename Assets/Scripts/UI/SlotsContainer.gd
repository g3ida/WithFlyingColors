extends CenterContainer

signal slot_pressed(id)

export var disable_empty_slots = false

onready var SaveSlot1Node = $HBoxContainer/SaveSlot1
onready var SaveSlot2Node = $HBoxContainer/SaveSlot2
onready var SaveSlot3Node = $HBoxContainer/SaveSlot3
onready var save_slots = [SaveSlot1Node, SaveSlot2Node, SaveSlot3Node]

var slots_colors = ["blue", "pink", "yellow"]

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
    save_slots[i].bg_color = ColorUtils.get_color(slots_colors[i])
    save_slots[i].id = i
    
  for i in range(save_slots.size()):
    if not save_slots[i].is_disabled:
      save_slots[i].has_focus = true
      break
    
func _process(_delta):
  pass #set_process = false

func _on_SaveSlot1_pressed():
  emit_signal("slot_pressed", 0)

func _on_SaveSlot2_pressed():
  emit_signal("slot_pressed", 1)

func _on_SaveSlot3_pressed():
  emit_signal("slot_pressed", 2)