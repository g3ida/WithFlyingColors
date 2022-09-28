extends Button

signal value_changed(value)
signal selection_changed(is_selected)

onready var SliderNode = $HSlider
onready var AnimationPlayerNode = $AnimationPlayer

onready var value = SliderNode.value setget set_value, get_value

var is_editing = false

func _ready():
  SliderNode.focus_mode = FOCUS_NONE
  focus_mode = FOCUS_ALL
  
func _input(_event):
  if has_focus():
    if Input.is_action_just_pressed("ui_accept"):
      _set_editing(!is_editing)
      get_tree().set_input_as_handled()
    elif Input.is_action_just_pressed("ui_cancel") and is_editing:
      _set_editing(false)
      get_tree().set_input_as_handled()
    elif is_editing:
      if Input.is_action_just_pressed("ui_left"):
        _on_left_pressed()
        get_tree().set_input_as_handled()
      elif Input.is_action_just_pressed("ui_right"):
        _on_right_pressed()
        get_tree().set_input_as_handled()
    
func _set_editing(_value):
  if not is_editing and _value:
    AnimationPlayerNode.stop()
    AnimationPlayerNode.play("Blink")
    _emit_selection_changed_signal()
  elif is_editing and not _value:
    AnimationPlayerNode.stop()
    AnimationPlayerNode.play("RESET")
    _emit_selection_changed_signal()
  is_editing = _value

func _on_left_pressed():
  _add_value_to_slider(-SliderNode.step)

func _on_right_pressed():
  _add_value_to_slider(SliderNode.step)

func _add_value_to_slider(_value):
  SliderNode.value = clamp(SliderNode.value + _value, SliderNode.min_value, SliderNode.max_value)
  _emit_value_changed_signal()

func _on_mouse_entered():
  grab_focus()

func _on_HSlider_drag_ended(_value_changed):
  _emit_value_changed_signal()
  
func _emit_value_changed_signal():
  emit_signal("value_changed", SliderNode.value)
  
func _emit_selection_changed_signal():
  emit_signal("selection_changed", is_editing)

func set_value(_value):
  SliderNode.value = _value

func get_value():
  return SliderNode.value
