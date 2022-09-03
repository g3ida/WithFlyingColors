extends Node2D

export (OpenSimplexNoise) var noise
export (float, 0, 1) var trauma = 0.0

export var max_x = 150
export var max_y = 150
export var max_r = 25

export var time_scale = 150
export (float, 0, 1) var decay = 0.6

var time = 0

onready var camera: Camera2D = get_parent()

func add_trauma(trauma_in):
    trauma = clamp(trauma + trauma_in, 0, 1)

# Called when the node enters the scene tree for the first time.
func _ready():
  pass


func _process(delta):
  time += delta
  var shake = pow(trauma, 2)
  camera.offset.x = noise.get_noise_3d(time * time_scale, 0, 0) * max_x * shake
  camera.offset.y = noise.get_noise_3d(0, time * time_scale, 0) * max_y * shake
  #need to activate camera rotation in order for this to function
  camera.rotation_degrees = noise.get_noise_3d(0, 0, time * time_scale) * max_r * shake
  
  if trauma > 0: trauma = clamp(trauma - (delta * decay), 0, 1)
