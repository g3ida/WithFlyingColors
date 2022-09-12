extends Node

#todo: need to take different dpi into consideration
const MOVE_DIRECTION_MIN_THRESHOLD = 7.5
const ROTATE_TOUCH_MAX_DELAY = 0.3
const ROTATE_TOUCH_MAX_DRAG_MODULE = 75.0
const ROTATE_TOUCH_MAX_DRAG_MODULE_SQUARED = ROTATE_TOUCH_MAX_DRAG_MODULE * ROTATE_TOUCH_MAX_DRAG_MODULE
const DASH_SWIPE_THRESHOLD = 180.0
const JUMP_SWIPE_THRESHOLD = 90.0
const JUMP_FORCE_NORMALIZATION_VALUE = 200.0

onready var viewport_size = get_viewport().get_size_override()
onready var screen_dpi = OS.get_screen_dpi(OS.current_screen) #fixme: does not work for ios?

var _move_Player_drag_input: InputEventScreenTouch
var swipe_module: SwipeModule
var rotate_left_condidates = []
var rotate_right_condidates = []

class TimedTouchEvent:
  var event: InputEventScreenTouch
  var time: float 
  func _init(_event, _time = 0):
    event = _event
    time = _time

func _ready():
  swipe_module = SwipeModule.new()
  swipe_module.DragWhileSwipe = true
  self.add_child(swipe_module)
  swipe_module.set_owner(self)

func _is_in_move_player_screen_position(pos: Vector2):
  return pos.x < viewport_size.x * 0.45

func _is_on_other_actions_direction(pos: Vector2):
  return pos.x > viewport_size.x * 0.55


func _input(event):
  if event is InputEventScreenTouch:
    if event.pressed:
      if _is_in_move_player_screen_position(event.position) and _move_Player_drag_input == null:
        _move_Player_drag_input = event
      if _is_in_rotate_left_position(event.position):
        rotate_left_condidates.append(TimedTouchEvent.new(event, 0))
      if _is_in_rotate_right_position(event.position):
        rotate_right_condidates.append(TimedTouchEvent.new(event, 0))
    else:
      if _move_Player_drag_input != null and _move_Player_drag_input.index == event.index:
        _move_Player_drag_input = null
        fire_move_event(Vector2.ZERO)
        
      if !rotate_left_condidates.empty():
        rotate_left_condidates.clear()
        fire_rotate_event(-1)
      if !rotate_right_condidates.empty():
        rotate_right_condidates.clear()
        fire_rotate_event(1)


  if event is InputEventScreenDrag:
    _hadle_rotation_action_with_drag(rotate_left_condidates, event)
    _hadle_rotation_action_with_drag(rotate_right_condidates, event)
    if _move_Player_drag_input != null:
      if event.index == _move_Player_drag_input.index:
        if event.relative.x > MOVE_DIRECTION_MIN_THRESHOLD:
          fire_move_event(Vector2.RIGHT)
        elif event.relative.x < -MOVE_DIRECTION_MIN_THRESHOLD:
          fire_move_event(Vector2.LEFT)

  if event is InputEventSwipe:
    event.set_heading(event.direction, 2.5)
    if event.up() and abs(event.direction.y) > JUMP_SWIPE_THRESHOLD:
      fire_jump_event(event)
    elif event.left() and abs(event.direction.x) > DASH_SWIPE_THRESHOLD and check_duration(event, 0.35):
      fire_dash_event(Vector2.LEFT)
    elif event.right() and abs(event.direction.x) > DASH_SWIPE_THRESHOLD and check_duration(event, 0.35):
      fire_dash_event(Vector2.RIGHT)
    elif event.down() and abs(event.direction.y) > DASH_SWIPE_THRESHOLD and check_duration(event, 0.55):
      fire_dash_event(Vector2.DOWN)

func _process(_delta):
  update_rotate_list(rotate_left_condidates, _delta)
  update_rotate_list(rotate_right_condidates, _delta)

func check_duration(ev: InputEventSwipe, duration: float):
  return ev.duration <= duration and ev.duration > Constants.EPSILON


func update_rotate_list(list, _delta):
  var trash_list = []
  var i = 0
  for el in list:
    el.time += _delta
    if el.time > ROTATE_TOUCH_MAX_DELAY:
      # items are inserted in reverse order to avoid IndexOutOfRangeExceptions
      trash_list.insert(0, i)
    i += 1
  for el in trash_list:
    list.remove(el)

func _is_in_rotate_right_position(pos: Vector2):
  return pos.x > viewport_size.x * 0.8 and pos.y < viewport_size.y * 0.72 and pos.y > viewport_size.y * 0.1

func _is_in_rotate_left_position(pos: Vector2):
  return pos.x > viewport_size.x * 0.5 and pos.x < viewport_size.x * 0.87 and pos.y > viewport_size.y * 0.72 

func _hadle_rotation_action_with_drag(list, drag: InputEventScreenDrag):
  var i = 0
  var list_to_remove = []
  for el in list:
    if el.event.index == drag.index:
      if (el.event.position - drag.position).length_squared() > ROTATE_TOUCH_MAX_DRAG_MODULE_SQUARED:
        # items are inserted in reverse order to avoid IndexOutOfRangeExceptions
        list_to_remove.insert(0, i)
      i += 1
  for el in list_to_remove:
    list.remove(el)

func fire_rotate_event(direction: int):
  var ev = InputTouchRotate.new()
  ev.direction = direction
  Input.parse_input_event(ev)

func fire_dash_event(direction: Vector2):
  var ev = InputTouchDash.new()
  ev.direction = direction
  Input.parse_input_event(ev)

func fire_jump_event(event):
  var ev = InputTouchJump.new()
  ev.force = clamp(-event.direction.y / JUMP_FORCE_NORMALIZATION_VALUE, 0.0, 1.0)
  Input.parse_input_event(ev)

func fire_move_event(direction: Vector2):
  var ev = InputTouchMove.new()
  ev.direction = direction
  Input.parse_input_event(ev)
