extends HBoxContainer

enum State {HIDDEN, HIDING, SHOWING, SHOWN}

signal select_button_pressed(slot_index)
signal clear_button_pressed(slot_index)

var slot_index = 0

onready var DeleteButtonNode = $DeleteButton
onready var ConfirmButtonNode = $ConfirmButton
onready var current_state = State.HIDDEN
onready var SpaceNode: Control = Control.new() 

func _ready():
  get_parent().call_deferred("add_child", SpaceNode)
  SpaceNode.call_deferred("set_owner", get_parent())
  update_space_node(0)

func show_button() -> void:
  if (_should_show_delete_button()):
    DeleteButtonNode.show_button()
    DeleteButtonNode.grab_focus()
  if (_should_show_select_button()):
    ConfirmButtonNode.show_button()
    ConfirmButtonNode.grab_focus()

func hide_button() -> void:
  DeleteButtonNode.hide_button()
  ConfirmButtonNode.hide_button()

func update_space_node(buttons_size) -> void:
  var buttons_max_width = DeleteButtonNode.BUTTON_MAX_WIDTH*2
  SpaceNode.rect_size.x = buttons_max_width - buttons_size
  SpaceNode.rect_min_size.x = buttons_max_width - buttons_size

func _process(_delta):
  var buttons_size = DeleteButtonNode.rect_size.x + ConfirmButtonNode.rect_size.x
  update_space_node(buttons_size)

func buttons_has_focus() -> bool:
  return DeleteButtonNode.button_has_focus() or ConfirmButtonNode.button_has_focus()

func _on_DeleteButton_pressed():
  emit_signal("clear_button_pressed", slot_index)

func _should_show_delete_button() -> bool:
  return SaveGame.is_slot_filled(slot_index)

func _should_show_select_button() -> bool:
  return SaveGame.current_slot_index != slot_index

func _on_ConfirmButton_pressed() -> void:
  emit_signal("select_button_pressed", slot_index)
