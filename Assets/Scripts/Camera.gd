extends Camera2D

const CAMERA_DRAG_JUMP = 0.45

onready var tweenNode = $Tween

var target_zoom: float = 1.0

onready var default_save_data = {
  "zoom_factor": 1.0,
  "bottom_limit": 10000,
  "top_limit": 0,
  "left_limit": 0,
  "right_limit": 10000,
  "drag_margin_bottom": Constants.DEFAULT_DRAG_MARGIN_TB,
  "drag_margin_left": Constants.DEFAULT_DRAG_MARGIN_LR,
  "drag_margin_right": Constants.DEFAULT_DRAG_MARGIN_LR,
  "drag_margin_top": Constants.DEFAULT_DRAG_MARGIN_TB,
}

var save_data = default_save_data

#used for tuning camera
var cached_drag_margin_top = drag_margin_top
var cached_drag_margin_bottom = drag_margin_bottom
var cached_drag_margin_left = drag_margin_left
var cached_drag_margin_right = drag_margin_right
  
func set_drag_margin_top(v):
  self.drag_margin_top = v
  cached_drag_margin_top = v

func set_drag_margin_bottom(v):
  self.drag_margin_bottom = v
  cached_drag_margin_bottom = v

func set_drag_margin_left(v):
  self.drag_margin_left = v
  cached_drag_margin_left = v

func set_drag_margin_right(v):
  self.drag_margin_right = v
  cached_drag_margin_right = v

func _ready():
  if is_current():
    Global.camera = self
  cache_drag_margins()

func _process(_delta):
  if is_current():
    Global.camera = self

func _on_checkpoint_hit(_checkpoint):
  save_data = {}
  save_data["zoom_factor"] = target_zoom
  save_data["bottom_limit"] = limit_bottom
  save_data["top_limit"] = limit_top
  save_data["left_limit"] = limit_left
  save_data["right_limit"] = limit_right
  save_data["drag_margin_bottom"] = cached_drag_margin_bottom
  save_data["drag_margin_left"] = cached_drag_margin_left
  save_data["drag_margin_right"] = cached_drag_margin_right
  save_data["drag_margin_top"] = cached_drag_margin_top

func reset():
  if save_data !=  null:
    zoom_by (save_data["zoom_factor"])
    limit_bottom = save_data["bottom_limit"]
    limit_top = save_data["top_limit"]
    limit_left = save_data["left_limit"]
    limit_right = save_data["right_limit"]
    self.drag_margin_bottom = save_data["drag_margin_bottom"]
    self.drag_margin_left = save_data["drag_margin_left"]
    self.drag_margin_right = save_data["drag_margin_right"]
    self.drag_margin_top = save_data["drag_margin_top"]

func save():
  if save_data != null:
    return save_data
  return default_save_data


func _on_player_jump():
  cache_drag_margins()
  if (drag_margin_bottom < CAMERA_DRAG_JUMP):
    self.drag_margin_bottom = CAMERA_DRAG_JUMP
  if (drag_margin_top < CAMERA_DRAG_JUMP):
    self.drag_margin_top = CAMERA_DRAG_JUMP

func _on_player_land():
  restore_drag_margins()

func cache_drag_margins():
  cached_drag_margin_bottom = drag_margin_bottom
  cached_drag_margin_top = drag_margin_top
  cached_drag_margin_left = drag_margin_left
  cached_drag_margin_right = drag_margin_right
  
func restore_drag_margins():
  drag_margin_bottom = cached_drag_margin_bottom
  drag_margin_top = cached_drag_margin_top
  drag_margin_left = cached_drag_margin_left
  drag_margin_right = cached_drag_margin_right
       
func zoom_by(factor: float):
  target_zoom = factor
  tweenNode.interpolate_property(self, "zoom", zoom, Vector2(factor, factor), 1.0)
  tweenNode.start()
  zoom = Vector2(factor, factor)
  
func connect_signals():
  var __ = Event.connect("checkpoint_reached", self, "_on_checkpoint_hit")
  __ = Event.connect("checkpoint_loaded", self, "reset")
  __ = Event.connect("player_jumped", self, "_on_player_jump")
  __ = Event.connect("player_land", self, "_on_player_land")

func disconnect_signals():
  Event.disconnect("checkpoint_reached", self, "_on_checkpoint_hit")
  Event.disconnect("checkpoint_loaded", self, "reset")
  Event.disconnect("player_jumped", self, "_on_player_jump")
  Event.disconnect("player_land", self, "_on_player_land")
      
func _enter_tree():
  connect_signals()

func _exit_tree():
  disconnect_signals()

func update_position(pos: Vector2):
  smoothing_enabled = false
  global_position = pos
  yield(get_tree(), "idle_frame")
  set_deferred("smoothing_enabled", true)
