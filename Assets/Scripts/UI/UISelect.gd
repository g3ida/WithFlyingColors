extends Button

export var SelectDriverScript: Script

onready var ChildContainerNode = $HBoxContainer
onready var LeftArrowNode = $HBoxContainer/Left
onready var RightArrowNode = $HBoxContainer/Right
onready var LabelNode = $HBoxContainer/Label

var select_driver
var index: int
var selected_value
var focus = false
var is_ready = false

signal Value_changed(value)

func update_rect_size():
  set_deferred("rect_min_size", ChildContainerNode.rect_size)
  set_deferred("rect_size", ChildContainerNode.rect_size)

func _process(_delta):
  pass
  #update_rect_size()

func _input(event):
  var focused_node = get_focus_owner()
  if focused_node != null and focused_node == self:
    if Input.is_action_just_pressed("ui_left"):
      _on_Left_pressed()
      get_tree().set_input_as_handled()
    elif Input.is_action_just_pressed("ui_right"):
      _on_Right_pressed()
      get_tree().set_input_as_handled()
    elif Input.is_action_just_pressed("ui_accept"):
      _on_Left_pressed()
      get_tree().set_input_as_handled()

func _ready():
  select_driver = SelectDriverScript.new()
  index = select_driver.get_default_selected_index()
  update_selected_item()
  update_rect_size()
  is_ready = true

func _on_Left_pressed():
  grab_focus()
  index = (index + 1) % select_driver.items.size()
  update_selected_item()
  emit_signal("Value_changed", select_driver.item_values[index])
  
func _on_Right_pressed():
  grab_focus()
  index = (index - 1) % select_driver.items.size()
  update_selected_item()
  emit_signal("Value_changed", select_driver.item_values[index])

func update_selected_item():
  LabelNode.text = select_driver.items[index]
  select_driver.on_item_selected(LabelNode.text)
  selected_value = select_driver.item_values[index]
  update_rect_size()

func _on_Button_mouse_entered():
  grab_focus()

func _on_Label_resized():
  if is_ready:
    update_rect_size()
