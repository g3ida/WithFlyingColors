extends GameMenu

const CENTER_CONTAINER_POS_Y = 405

onready var BackButtonNode = $BackButton
onready var LoadTextNode = $LOAD
onready var SlotsContainer = $SlotsContainer
onready var EmptySlotDialogNode = $EmptySlotDialog
onready var ColorRectNode = $ColorRect

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
  $SlotsContainer.rect_position.y = pos_y

func _on_SlotsContainer_slot_pressed(id):
  if SaveGame.is_slot_filled(id):
    SaveGame.current_slot_index = id
    Event.emit_menu_button_pressed(MenuManager.Menus.LOAD)
    navigate_to_screen(MenuManager.Menus.GAME)
  else:
    _show_dialog()

func on_menu_button_pressed(menu_button):
  if menu_button == MenuManager.Menus.GAME:
    navigate_to_screen(MenuManager.Menus.GAME)
    return true
  return false

func _show_dialog():
    EmptySlotDialogNode.show()
    ColorRectNode.show()
    handle_back_event = false
    
func _hide_dialog():
  if EmptySlotDialogNode != null:
    EmptySlotDialogNode.hide()
    ColorRectNode.hide()
    handle_back_event = true
  
func process(_event):
  if Input.is_action_just_pressed("ui_cancel") and EmptySlotDialogNode.visible:
    call_deferred("_hide_dialog")
    
