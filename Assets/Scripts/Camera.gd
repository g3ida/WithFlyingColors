extends Camera2D

const CAMERA_DRAG_JUMP = 0.45

onready var tweenNode = $Tween

var target_zoom: float = 1.0

var saved_zoom_factor: float = 1.0
var saved_bottom_limit: int = 10000
var saved_top_limit: int = 0
var saved_left_limit: int = 0
var saved_right_limit: int = 10000
var saved_drag_margin_bottom = Constants.DEFAULT_DRAG_MARGIN_TB
var saved_drag_margin_left = Constants.DEFAULT_DRAG_MARGIN_LR
var saved_drag_margin_right = Constants.DEFAULT_DRAG_MARGIN_LR
var saved_drag_margin_top = Constants.DEFAULT_DRAG_MARGIN_TB

#used for tuning camera
var cached_drag_margin_top = drag_margin_top
var cached_drag_margin_bottom = drag_margin_bottom
var cached_drag_margin_left = drag_margin_left
var cached_drag_margin_right = drag_margin_right

var did_checkpoint_hit = false
  
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
  saved_zoom_factor = target_zoom
  saved_bottom_limit = limit_bottom
  saved_top_limit = limit_top
  saved_left_limit = limit_left
  saved_right_limit = limit_right
  did_checkpoint_hit = true
  saved_drag_margin_bottom = cached_drag_margin_bottom
  saved_drag_margin_left = cached_drag_margin_left
  saved_drag_margin_right = cached_drag_margin_right
  saved_drag_margin_top = cached_drag_margin_top

func reset():
  if (did_checkpoint_hit):
    zoom_by (saved_zoom_factor)
    limit_bottom = saved_bottom_limit
    limit_top = saved_top_limit
    limit_left = saved_left_limit
    limit_right = saved_right_limit
    self.drag_margin_bottom = saved_drag_margin_bottom
    self.drag_margin_left = saved_drag_margin_left
    self.drag_margin_right = saved_drag_margin_right
    self.drag_margin_top = saved_drag_margin_top

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
