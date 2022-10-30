extends Node2D

enum CamLimitEnum {
  NO_LIMITS,
  FULL_LIMIT,
  LIMIT_BOTTOM_RIGHT,
  LIMIT_BOTTOM_LEFT,
  LIMIT_TOP_RIGHT,
  LIMIT_TOP_LEFT,
  LIMIT_X_AXIS,
  LIMIT_Y_AXIS,
  LIMIT_ALL_BUT_TOP,
  LIMIT_ALL_BUT_LEFT,
  LIMIT_ALL_BUT_RIGHT,
  LIMIT_ALL_BUT_BOTTOM,
  LIMIT_LEFT,
  LIMIT_RIGHT,
  LIMIT_TOP,
  LIMIT_BOTTOM,
}

const X_AXIS_LIMITS = [
  CamLimitEnum.FULL_LIMIT,
  CamLimitEnum.LIMIT_X_AXIS,
  CamLimitEnum.LIMIT_ALL_BUT_TOP,
  CamLimitEnum.LIMIT_ALL_BUT_BOTTOM,
]
const Y_AXIS_LIMITS = [
  CamLimitEnum.FULL_LIMIT,
  CamLimitEnum.LIMIT_Y_AXIS,
  CamLimitEnum.LIMIT_ALL_BUT_LEFT,
  CamLimitEnum.LIMIT_ALL_BUT_RIGHT,
]

export var full_viewport_drag_margin = false
export(CamLimitEnum) var position_clipping_mode := CamLimitEnum.FULL_LIMIT
export var zoom = 1.0
export(NodePath) var follow_node = null

#this two variables are usefull to recalculate the positions of the applied limits
#to match the size of the viewport
export var limit_x_axis_to_view_size = false
export var limit_y_axis_to_view_size = false

var areaNodes = []
var positionsNodes = []

var top: int
var bottom: int
var left: int
var right: int

func _ready():
  for ch in get_children():
    if ch is Position2D:
      positionsNodes.append(ch)
    elif ch is Area2D:
      areaNodes.append(ch)
      var __ = ch.connect("body_entered", self, "_on_body_entred")
  setup_limiting()

func setup_limiting():
  if position_clipping_mode == CamLimitEnum.NO_LIMITS:
    pass

  elif position_clipping_mode == CamLimitEnum.FULL_LIMIT:
    if positionsNodes.size() != 2:
      push_error("position liminting FULL mode require you add two child position nodes")
    else:
      top = int(min(positionsNodes[0].global_position.y, positionsNodes[1].global_position.y))
      bottom = int(max(positionsNodes[0].global_position.y, positionsNodes[1].global_position.y))
      left = int(min(positionsNodes[0].global_position.x, positionsNodes[1].global_position.x))
      right = int(max(positionsNodes[0].global_position.x, positionsNodes[1].global_position.x))
  
  elif position_clipping_mode == CamLimitEnum.LIMIT_BOTTOM_RIGHT:
    if positionsNodes.size() != 1:
      push_error("position liminting LIMIT_BOTTOM_RIGHT mode require you add ONLY one child position node")
    else:
      bottom = positionsNodes[0].global_position.y
      right = positionsNodes[0].global_position.x
  
  elif position_clipping_mode == CamLimitEnum.LIMIT_BOTTOM_LEFT:
    if positionsNodes.size() != 1:
      push_error("position liminting LIMIT_BOTTOM_LEFT mode require you add ONLY one child position node")
    else:
      bottom = positionsNodes[0].global_position.y
      left = positionsNodes[0].global_position.x

  elif position_clipping_mode == CamLimitEnum.LIMIT_TOP_RIGHT:
    if positionsNodes.size() != 1:
      push_error("position liminting LIMIT_TOP_RIGHT mode require you add ONLY one child position node")
    else:
      top = positionsNodes[0].global_position.y
      right = positionsNodes[0].global_position.x

  elif position_clipping_mode == CamLimitEnum.LIMIT_TOP_LEFT:
    if positionsNodes.size() != 1:
      push_error("position liminting LIMIT_TOP_LEFT mode require you add ONLY one child position node")
    else:
      top = positionsNodes[0].global_position.y
      left = positionsNodes[0].global_position.x

  elif position_clipping_mode == CamLimitEnum.LIMIT_X_AXIS:
    if positionsNodes.size() != 1:
      push_error("position liminting LIMIT_X_AXIS mode require you add ONLY one child position node")
    else:
      left = int(min(positionsNodes[0].global_position.x, positionsNodes[1].global_position.x))
      right = int(max(positionsNodes[0].global_position.x, positionsNodes[1].global_position.x))

  elif position_clipping_mode == CamLimitEnum.LIMIT_Y_AXIS:
    if positionsNodes.size() != 1:
      push_error("position liminting LIMIT_Y_AXIS mode require you add ONLY one child position node")
    else:
      top = int(min(positionsNodes[0].global_position.y, positionsNodes[1].global_position.y))
      bottom = int(max(positionsNodes[0].global_position.y, positionsNodes[1].global_position.y))

  elif position_clipping_mode == CamLimitEnum.LIMIT_ALL_BUT_TOP:
    if positionsNodes.size() != 2:
      push_error("position liminting ALL BUT TOP mode require you add two child position nodes")
    else:
      bottom = int(max(positionsNodes[0].global_position.y, positionsNodes[1].global_position.y))
      left = int(min(positionsNodes[0].global_position.x, positionsNodes[1].global_position.x))
      right = int(max(positionsNodes[0].global_position.x, positionsNodes[1].global_position.x))
  
  elif position_clipping_mode == CamLimitEnum.LIMIT_ALL_BUT_BOTTOM:
    if positionsNodes.size() != 2:
      push_error("position liminting ALL BUT BOTTOM mode require you add two child position nodes")
    else:
      top = int(min(positionsNodes[0].global_position.y, positionsNodes[1].global_position.y))
      left = int(min(positionsNodes[0].global_position.x, positionsNodes[1].global_position.x))
      right = int(max(positionsNodes[0].global_position.x, positionsNodes[1].global_position.x))
  
  elif position_clipping_mode == CamLimitEnum.LIMIT_ALL_BUT_LEFT:
    if positionsNodes.size() != 2:
      push_error("position liminting ALL BUT LEFT mode require you add two child position nodes")
    else:
      top = int(min(positionsNodes[0].global_position.y, positionsNodes[1].global_position.y))
      bottom = int(max(positionsNodes[0].global_position.y, positionsNodes[1].global_position.y))
      right = int(max(positionsNodes[0].global_position.x, positionsNodes[1].global_position.x))
    
  elif position_clipping_mode == CamLimitEnum.LIMIT_ALL_BUT_RIGHT:
    if positionsNodes.size() != 2:
      push_error("position liminting ALL BUT RIGHT mode require you add two child position nodes")
    else:
      top = int(min(positionsNodes[0].global_position.y, positionsNodes[1].global_position.y))
      bottom = int(max(positionsNodes[0].global_position.y, positionsNodes[1].global_position.y))
      left = int(min(positionsNodes[0].global_position.x, positionsNodes[1].global_position.x))

  elif position_clipping_mode == CamLimitEnum.LIMIT_LEFT:
    if positionsNodes.size() != 1:
      push_error("position liminting LIMIT_LEFT mode require you add one child position node")
    left = positionsNodes[0].global_position.x
  
  elif position_clipping_mode == CamLimitEnum.LIMIT_RIGHT:
    if positionsNodes.size() != 1:
      push_error("position liminting LIMIT_RIGHT mode require you add one child position node")
    right = positionsNodes[0].global_position.x
  
  elif position_clipping_mode == CamLimitEnum.LIMIT_TOP:
    if positionsNodes.size() != 1:
      push_error("position liminting LIMIT_TOP mode require you add one child position node")
    top = positionsNodes[0].global_position.y
  
  elif position_clipping_mode == CamLimitEnum.LIMIT_BOTTOM:
    if positionsNodes.size() != 1:
      push_error("position liminting LIMIT_BOTTOM mode require you add one child position node")
    bottom = positionsNodes[0].global_position.y
   

func _adapt_limits_to_screen_size():
  var viewport_rect = get_viewport().get_visible_rect().size
  var inv_zoom = 1.0 / zoom
  if limit_x_axis_to_view_size and position_clipping_mode in X_AXIS_LIMITS:
    var diff = (right - left)*inv_zoom - viewport_rect.x
    left += diff*0.5*(zoom)
    right = left + viewport_rect.x * zoom
  if limit_y_axis_to_view_size and position_clipping_mode in Y_AXIS_LIMITS:
    var diff = (bottom - top)*inv_zoom - viewport_rect.y
    bottom -= diff*0.5*(zoom)
    top = bottom - viewport_rect.y * zoom
    
func _on_body_entred(_body):
  if _body == Global.player:
    apply_camera_changes()

func apply_camera_changes():
  set_follow_node()
  set_camera_limits()
  set_camera_drag_margins()
  Global.camera.zoom_by(zoom)
  
func set_follow_node():
  if follow_node != null:
    var node = get_node(follow_node)
    if node != null:
      Global.camera.set_follow_node(node)

func set_camera_limits():
  _adapt_limits_to_screen_size()
  if position_clipping_mode == CamLimitEnum.NO_LIMITS:
    Global.camera.limit_left = Constants.DEFAULT_CAMERA_LIMIT_LEFT
    Global.camera.limit_bottom = Constants.DEFAULT_CAMERA_LIMIT_BOTTOM
    Global.camera.limit_right = Constants.DEFAULT_CAMERA_LIMIT_RIGHT
    Global.camera.limit_top = Constants.DEFAULT_CAMERA_LIMIT_TOP
  if position_clipping_mode == CamLimitEnum.FULL_LIMIT:
    Global.camera.limit_left = left
    Global.camera.limit_bottom = bottom
    Global.camera.limit_right = right
    Global.camera.limit_top = top
  elif position_clipping_mode == CamLimitEnum.LIMIT_BOTTOM_RIGHT:
    Global.camera.limit_left = Constants.DEFAULT_CAMERA_LIMIT_LEFT
    Global.camera.limit_bottom = bottom
    Global.camera.limit_right = right
    Global.camera.limit_top = Constants.DEFAULT_CAMERA_LIMIT_TOP
  elif position_clipping_mode == CamLimitEnum.LIMIT_BOTTOM_LEFT:
    Global.camera.limit_left = left
    Global.camera.limit_bottom = bottom
    Global.camera.limit_right = Constants.DEFAULT_CAMERA_LIMIT_RIGHT
    Global.camera.limit_top = Constants.DEFAULT_CAMERA_LIMIT_TOP
  elif position_clipping_mode == CamLimitEnum.LIMIT_TOP_RIGHT:
    Global.camera.limit_left = Constants.DEFAULT_CAMERA_LIMIT_LEFT
    Global.camera.limit_bottom = Constants.DEFAULT_CAMERA_LIMIT_BOTTOM
    Global.camera.limit_right = right
    Global.camera.limit_top = top
  elif position_clipping_mode == CamLimitEnum.LIMIT_TOP_LEFT:
    Global.camera.limit_left = left
    Global.camera.limit_bottom = Constants.DEFAULT_CAMERA_LIMIT_BOTTOM
    Global.camera.limit_right = Constants.DEFAULT_CAMERA_LIMIT_RIGHT
    Global.camera.limit_top = top
  elif position_clipping_mode == CamLimitEnum.LIMIT_X_AXIS:
    Global.camera.limit_left = left
    Global.camera.limit_bottom = Constants.DEFAULT_CAMERA_LIMIT_BOTTOM
    Global.camera.limit_right = right
    Global.camera.limit_top = Constants.DEFAULT_CAMERA_LIMIT_TOP
  elif position_clipping_mode == CamLimitEnum.LIMIT_Y_AXIS:
    Global.camera.limit_left = Constants.DEFAULT_CAMERA_LIMIT_LEFT
    Global.camera.limit_bottom = bottom
    Global.camera.limit_right = Constants.DEFAULT_CAMERA_LIMIT_RIGHT
    Global.camera.limit_top = top
  elif position_clipping_mode == CamLimitEnum.LIMIT_ALL_BUT_TOP:
    Global.camera.limit_left = left
    Global.camera.limit_bottom = bottom
    Global.camera.limit_right = right
    Global.camera.limit_top = Constants.DEFAULT_CAMERA_LIMIT_TOP
  elif position_clipping_mode == CamLimitEnum.LIMIT_ALL_BUT_LEFT:
    Global.camera.limit_left = Constants.DEFAULT_CAMERA_LIMIT_LEFT
    Global.camera.limit_bottom = bottom
    Global.camera.limit_right = right
    Global.camera.limit_top = top
  elif position_clipping_mode == CamLimitEnum.LIMIT_ALL_BUT_RIGHT:
    Global.camera.limit_left = left
    Global.camera.limit_bottom = bottom
    Global.camera.limit_right = Constants.DEFAULT_CAMERA_LIMIT_RIGHT
    Global.camera.limit_top = top
  elif position_clipping_mode == CamLimitEnum.LIMIT_ALL_BUT_BOTTOM:
    Global.camera.limit_left = left
    Global.camera.limit_bottom = Constants.DEFAULT_CAMERA_LIMIT_BOTTOM
    Global.camera.limit_right = right
    Global.camera.limit_top = top
  elif position_clipping_mode == CamLimitEnum.LIMIT_LEFT:
    Global.camera.limit_left = left
  elif position_clipping_mode == CamLimitEnum.LIMIT_RIGHT:
    Global.camera.limit_right = right
  elif position_clipping_mode == CamLimitEnum.LIMIT_TOP:
    Global.camera.limit_top = top
  elif position_clipping_mode == CamLimitEnum.LIMIT_BOTTOM:
    Global.camera.limit_bottom = bottom
    
func set_camera_drag_margins():
  if full_viewport_drag_margin:
    Global.camera.set_drag_margin_bottom(1)
    Global.camera.set_drag_margin_left(1)
    Global.camera.set_drag_margin_right(1)
    Global.camera.set_drag_margin_top(1)
  else:
    Global.camera.set_drag_margin_bottom(Constants.DEFAULT_DRAG_MARGIN_TB)
    Global.camera.set_drag_margin_left(Constants.DEFAULT_DRAG_MARGIN_LR)
    Global.camera.set_drag_margin_right(Constants.DEFAULT_DRAG_MARGIN_LR)
    Global.camera.set_drag_margin_top(Constants.DEFAULT_DRAG_MARGIN_TB)

# uncomment this function to draw the limits on the screen
# func _draw():
#   draw_line(
#     Vector2(Constants.DEFAULT_CAMERA_LIMIT_LEFT, top),
#     Vector2(Constants.DEFAULT_CAMERA_LIMIT_RIGHT, top), Color.red, 2.0)
  
#   draw_line(
#     Vector2(Constants.DEFAULT_CAMERA_LIMIT_LEFT, bottom),
#     Vector2(Constants.DEFAULT_CAMERA_LIMIT_RIGHT, bottom), Color.green, 2.0)
  
#   draw_line(
#     Vector2(right, Constants.DEFAULT_CAMERA_LIMIT_TOP),
#     Vector2(right, Constants.DEFAULT_CAMERA_LIMIT_BOTTOM), Color.yellow, 2.0)
  
#   draw_line(
#     Vector2(left, Constants.DEFAULT_CAMERA_LIMIT_TOP),
#     Vector2(left, Constants.DEFAULT_CAMERA_LIMIT_BOTTOM), Color.blue, 2.0)
