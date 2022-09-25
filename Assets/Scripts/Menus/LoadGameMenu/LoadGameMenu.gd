extends GameMenu

const SelectArrowScene = preload("res://Assets/Scenes/UI/SaveSlotArrow.tscn")

onready var BackButtonNode = $BackButton
onready var LoadTextNode = $LOAD
onready var SaveSlot1Node = $CenterContainer/HBoxContainer/SaveSlot1
onready var SaveSlot2Node = $CenterContainer/HBoxContainer/SaveSlot2
onready var SaveSlot3Node = $CenterContainer/HBoxContainer/SaveSlot3
onready var CenterContainerNode = $CenterContainer

const CENTER_CONTAINER_POS_Y = 405

onready var save_slots = [SaveSlot1Node, SaveSlot2Node, SaveSlot3Node]

var slots_colors = ["blue", "pink", "yellow"]
var SelectArrowNode = null
var currently_selected_slot = null

func _spawn_arrow_node():
  if SelectArrowNode != null:
    SelectArrowNode.queue_free()
  SelectArrowNode = SelectArrowScene.instance()
  CenterContainerNode.add_child(SelectArrowNode)
  SelectArrowNode.set_owner(CenterContainerNode)

func process(_delta):
  .process(_delta)
  for ss in save_slots:
    if ss.has_focus:
      if currently_selected_slot != ss:
        SelectArrowNode.move_to(ss.rect_position + Vector2(ss.rect_size.x * 0.5, -3))
        currently_selected_slot = ss
      break
  
func _ready():
  .ready()
  for i in range(save_slots.size()):
    save_slots[i].texture = SaveGame.load_slot_image(i)
    var meta_data = SaveGame.get_slot_meta_data(i)
    if meta_data != null:
      save_slots[i].timestamp = meta_data["save_time"]
    else:
      save_slots[i].description = "<EMPTY>"
    save_slots[i].bg_color = ColorUtils.get_color(slots_colors[i])

  _spawn_arrow_node()

func on_enter():
  animators.append(init_control_element_animator($BackButton, 2*DELAY))
  animators.append(init_control_element_animator($LOAD, DELAY))
  animators.append(init_slots_animator(DELAY))
  for animator in animators:
    animator.update(0)

func on_exit():
  reverse_animators()

func is_exit_ceremony_done() -> bool:
  return animators_done()

func is_enter_ceremony_done() -> bool:
  return animators_done()
  
func _on_BackButton_pressed():
  Event.emit_menu_button_pressed(MenuButtons.BACK)
  
func init_slots_animator(delay: float) -> Animator:
  var start = CENTER_CONTAINER_POS_Y + DISTANCE
  var end = CENTER_CONTAINER_POS_Y
  var interpolation = PowInterpolation.new(2)
  var duration = DURATION
  return Animator.new(start, end, funcref(self, "update_slots_y_pos"), duration, delay, interpolation)

func update_slots_y_pos(pos_y):
  $CenterContainer.rect_position.y = pos_y
