extends CenterContainer

const SelectArrowScene = preload("res://Assets/Scenes/UI/SaveSlotArrow.tscn")

onready var SaveSlot1Node = $HBoxContainer/SaveSlot1
onready var SaveSlot2Node = $HBoxContainer/SaveSlot2
onready var SaveSlot3Node = $HBoxContainer/SaveSlot3
onready var save_slots = [SaveSlot1Node, SaveSlot2Node, SaveSlot3Node]

var slots_colors = ["blue", "pink", "yellow"]
var SelectArrowNode = null
var currently_selected_slot = null

func _spawn_arrow_node():
  if SelectArrowNode != null:
    SelectArrowNode.queue_free()
  SelectArrowNode = SelectArrowScene.instance()
  add_child(SelectArrowNode)
  SelectArrowNode.set_owner(self)

func _ready():
  for i in range(save_slots.size()):
    save_slots[i].texture = SaveGame.load_slot_image(i)
    var meta_data = SaveGame.get_slot_meta_data(i)
    if meta_data != null:
      save_slots[i].timestamp = meta_data["save_time"]
    else:
      save_slots[i].description = "<EMPTY>"
    save_slots[i].bg_color = ColorUtils.get_color(slots_colors[i])
  _spawn_arrow_node()

func _process(_delta):
  for ss in save_slots:
    if ss.has_focus:
      if currently_selected_slot != ss:
        SelectArrowNode.move_to(ss.rect_position + Vector2(ss.rect_size.x * 0.5, -3))
        currently_selected_slot = ss
      break
