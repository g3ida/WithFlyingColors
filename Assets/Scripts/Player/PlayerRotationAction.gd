class_name PlayerRotationAction
const CountdownTimer = preload("res://Assets/Scripts/Utils/CountdownTimer.cs")

const DEFAULT_ROTATION_DURATION = 0.1
var duration: float
var thetaZero = 0.0 # initial angle, before the rotation is performed.
var thetaTarget = 0.0 # target angle, after the rotation is completed.
var thetaPoint = 0 # the calculated angule.
var rotationTimer = CountdownTimer.new()
var canRotate = true # set to false when rotation is in progress.
var body: KinematicBody2D
  
func _init(_body: Node2D):
  rotationTimer.Set(DEFAULT_ROTATION_DURATION, false)
  self.body = _body
  
func step(delta: float):
  if rotationTimer.IsRunning():
    rotationTimer.Step(delta)
    if not rotationTimer.IsRunning():
      # last frame correction
      var currentAngle = body.rotation
      thetaPoint = (thetaTarget - currentAngle) / delta
      rotationTimer.Stop()
    body.rotate(thetaPoint * delta)
  elif not canRotate:
    thetaPoint = 0
    rotationTimer.Stop()
    canRotate = true
    
func execute(direction: int, # -1 left 1 right (can be removed since I added the angle param)
  angle_radians: float = Constants.PI2,
  _duration = DEFAULT_ROTATION_DURATION,
  should_force = true,
  cumulate_target = true,
  use_round = true):

  if !canRotate and !should_force: return false
  canRotate = false
  self.duration = _duration
  rotationTimer.Set(duration, false)
  
  thetaZero = body.rotation
  
  if abs(thetaPoint) > Constants.EPSILON and cumulate_target:
    thetaZero = thetaTarget

  var unrounded_angle = deg2rad(rad2deg(thetaZero + direction*angle_radians))/angle_radians
  if (use_round):
    thetaTarget = round(unrounded_angle) * angle_radians
  else:
    var rounded_angle = ceil(unrounded_angle) if direction == -1 else floor(unrounded_angle)
    thetaTarget = rounded_angle * angle_radians

  if abs(thetaPoint) > Constants.EPSILON and cumulate_target:
    thetaZero = body.rotation

  thetaPoint = (thetaTarget - thetaZero) / duration
  rotationTimer.Reset()
  return true
