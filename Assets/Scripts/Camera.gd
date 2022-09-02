extends Camera2D

onready var tweenNode = $Tween

var target_zoom: float = 1.0
var saved_zoom_factor: float = 1.0
var saved_bottom_limit: int = 10000
var saved_top_limit: int = 0
var saved_left_limit: int = 0
var saved_right_limit: int = 10000

var did_checkpoint_hit = false

func _ready():
  if is_current():
    Global.camera = self

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
  
func reset():
  if (did_checkpoint_hit):
    zoom_by (saved_zoom_factor)
    limit_bottom = saved_bottom_limit
    limit_top = saved_top_limit
    limit_left = saved_left_limit
    limit_right = saved_right_limit
       
func zoom_by(factor: float):
  target_zoom = factor
  tweenNode.interpolate_property(self, "zoom", zoom, Vector2(factor, factor), 1.0)
  tweenNode.start()
  zoom = Vector2(factor, factor)
  
func connect_signals():
  var __ = Event.connect("checkpoint_reached", self, "_on_checkpoint_hit")
  __ = Event.connect("checkpoint_loaded", self, "reset")
  
func disconnect_signals():
  Event.disconnect("checkpoint_reached", self, "_on_checkpoint_hit")
  Event.disconnect("checkpoint_loaded", self, "reset")
      
func _enter_tree():
  connect_signals()

func _exit_tree():
  disconnect_signals()
