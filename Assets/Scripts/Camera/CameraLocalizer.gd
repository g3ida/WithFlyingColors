extends Node2D

enum CamLimitEnum {
  FULL_LIMIT,
  LIMIT_BOTTOM_RIGHT,
  LIMIT_BOTTOM_LEFT,
  LIMIT_TOP_RIGHT,
  LIMIT_TOP_LEFT,
  LIMIT_X_AXIS,
  LIMIT_Y_AXIS
}

export var full_viewport_drag_margin = false
export(CamLimitEnum) var position_clipping_mode := CamLimitEnum.FULL_LIMIT

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
  if position_clipping_mode == CamLimitEnum.FULL_LIMIT:
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
      push_error("position liminting LIMIT_X_AXIS mode require you add two child position nodes")
    else:
      left = int(min(positionsNodes[0].global_position.x, positionsNodes[1].global_position.x))
      right = int(max(positionsNodes[0].global_position.x, positionsNodes[1].global_position.x))

  elif position_clipping_mode == CamLimitEnum.LIMIT_Y_AXIS:
    if positionsNodes.size() != 1:
      push_error("position liminting LIMIT_Y_AXIS mode require you add two child position nodes")
    else:
      top = int(min(positionsNodes[0].global_position.y, positionsNodes[1].global_position.y))
      bottom = int(max(positionsNodes[0].global_position.y, positionsNodes[1].global_position.y))

func _on_body_entred(_body):
  if _body == Global.player:
    set_camera_limits()
    set_camera_drag_margins()
  
func set_camera_limits():
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
    print(left, " ", bottom)
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