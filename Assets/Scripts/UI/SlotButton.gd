class_name SlotButton
extends ColorRect

signal pressed()

enum State {HIDDEN, HIDING, SHOWING, SHOWN}

const BUTTON_MAX_WIDTH = 110
const BUTTON_MIN_WIDTH = 0
const MOVE_DURATION = 0.15

export var node_color = "pink"
export var iconTexture: Texture
export var focus_left_node: NodePath
export var focus_right_node: NodePath

onready var TweenNode = $Tween
onready var ButtonNode = $Button
onready var BlinkAnimationPlayerNode = $BlinkAnimationPlayer
onready var current_state = State.HIDDEN

func _ready():
  self.ButtonNode.rect_size.x = 0
  self.visible = false
  self.ButtonNode.disabled = true
  update_height()
  self.color = ColorUtils.get_basic_color(ColorUtils.get_group_color_index(node_color))
  self.ButtonNode.icon = iconTexture
  
func _set_focus_next_and_previous():
  if not self.focus_left_node.is_empty():
    var lnode = get_node(self.focus_left_node)
    if lnode != null and lnode.ButtonNode != null:
      self.ButtonNode.focus_neighbour_left = lnode.ButtonNode.get_path()
  if not self.focus_right_node.is_empty():
    var rnode = get_node(self.focus_right_node)
    if rnode != null and rnode.ButtonNode != null:
      self.ButtonNode.focus_neighbour_right = rnode.ButtonNode.get_path()

func show_button():
  if current_state == State.HIDING or current_state == State.HIDDEN:
    self.visible = true
    self.ButtonNode.disabled = false
    current_state = State.SHOWING
    setup_tween(BUTTON_MAX_WIDTH)
    ButtonNode.set_focus_mode(Control.FOCUS_ALL)
    _set_focus_next_and_previous()
    
func button_has_focus():
  return ButtonNode.has_focus()
  
func hide_button():
  if current_state == State.SHOWING or current_state == State.SHOWN:
    current_state = State.HIDING
    setup_tween(BUTTON_MIN_WIDTH)
    BlinkAnimationPlayerNode.play("RESET")
    ButtonNode.set_focus_mode(Control.FOCUS_NONE)

func grab_focus():
  ButtonNode.grab_focus()
  BlinkAnimationPlayerNode.play("Blink") 
  
func update_height():
  #make the button height expand the whole container
  self.rect_size.y = get_parent().rect_size.y
  self.ButtonNode.rect_size.y = self.rect_size.y

func _process(_delta):
  self.rect_min_size.x = ButtonNode.rect_min_size.x
  self.rect_size.x = ButtonNode.rect_min_size.x
  update_height()
  blink_button_if_needed()

func blink_button_if_needed():
  if ButtonNode.has_focus():
    if BlinkAnimationPlayerNode.current_animation != "Blink":
      BlinkAnimationPlayerNode.play("Blink")
  else:
    if BlinkAnimationPlayerNode.current_animation == "Blink":
      BlinkAnimationPlayerNode.play("RESET")

func setup_tween(size_x):
  TweenNode.remove_all()
  TweenNode.interpolate_property(
    ButtonNode,
    "rect_min_size:x",
    ButtonNode.rect_min_size.x,
    size_x,
    MOVE_DURATION,
    Tween.TRANS_LINEAR,
    Tween.EASE_IN_OUT)
  TweenNode.start()

func _on_Tween_tween_completed(_object, _key):
  if current_state == State.HIDING:
    current_state = State.HIDDEN
    self.visible = false
    self.ButtonNode.disabled = true
  elif current_state == State.SHOWING:
    current_state = State.SHOWN

func _on_Button_pressed():
  emit_signal("pressed")

func _on_Button_mouse_entered():
  grab_focus()
