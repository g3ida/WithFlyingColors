extends Button

export var SelectDriverScript: Script

var select_driver
var index: int
var selected_value
var focus = false

func update_rect_size():
  rect_size = $HBoxContainer.rect_size

func _process(_delta):
  update_rect_size()
  
  var focused_node = get_focus_owner();
  if focused_node != null and focused_node == self:
    if Input.is_action_just_pressed("ui_accept"):
      _on_Left_pressed()

onready var ui_label = $HBoxContainer/Label
onready var ui_left = $HBoxContainer/Left
onready var ui_right = $HBoxContainer/Right

signal Value_changed(value)

func _ready():
  select_driver = SelectDriverScript.new()
  index = select_driver.get_default_selected_index()
  update_selected_item()
  update_rect_size()

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
  ui_label.text = select_driver.items[index]
  select_driver.on_item_selected(ui_label.text)
  selected_value = select_driver.item_values[index]

func _on_Button_mouse_entered():
  grab_focus()
