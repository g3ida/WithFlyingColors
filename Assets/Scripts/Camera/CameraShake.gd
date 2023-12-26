extends Node2D

const TRANS = Tween.TRANS_SINE
const EASE = Tween.EASE_IN_OUT

onready var DurationNode = $Duration
onready var FrequencyNode = $Frequency

var tweener: SceneTreeTween

var amplitude
var priority = 0

onready var camera: Camera2D = get_parent()

func start(_duration = 0.15, _frequency = 10.0, _amplitude = 10, _priority = 0):
  if _priority >= self.priority:
    self.priority = _priority
    self.amplitude = _amplitude
    DurationNode.wait_time = _duration
    FrequencyNode.wait_time = 1.0 / _frequency
    DurationNode.start()
    FrequencyNode.start()
    _new_shake()


func camera_tween_interpolate(v: Vector2):
  if tweener:
    tweener.kill()
  tweener = create_tween()
  var __ = tweener.tween_property(
    camera,
    "offset",
    v,
    FrequencyNode.wait_time
  ).from(camera.offset
  ).set_trans(TRANS
  ).set_ease(EASE)

func _new_shake():
  var rand = Vector2()
  rand.x = rand_range(-amplitude, amplitude)
  rand.y = rand_range(-amplitude, amplitude)
  camera_tween_interpolate(rand)

func _finish_shake():
  camera_tween_interpolate(Vector2())
  priority = 0

func _on_Frequency_timeout():
  _new_shake()

func _on_Duration_timeout():
  _finish_shake()
  FrequencyNode.stop()
