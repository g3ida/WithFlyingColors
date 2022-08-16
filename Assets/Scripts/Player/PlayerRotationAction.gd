class_name PlayerRotationAction
const CountdownTimer = preload("res://Assets/Scripts/Utils/CountdownTimer.gd")

const ROTATION_DURATION = 0.12
var thetaZero = 0.0 # initial angle, before the rotation is performed.
var thetaTarget = 0.0 # target angle, after the rotation is completed.
var thetaPoint = 0 # the calculated angule.
var rotationTimer = CountdownTimer.new(ROTATION_DURATION, false)
var canRotate = true # set to false when rotation is in progress.
var body: KinematicBody2D
	
func _init(body: Node2D):
	self.body = body
	
func step(delta: float):
	if rotationTimer.is_running():
		rotationTimer.step(delta)
		if not rotationTimer.is_running():
			# last frame correction
			var currentAngle = body.rotation
			thetaPoint = (thetaTarget - currentAngle) / delta
			rotationTimer.stop()
		body.rotate(thetaPoint * delta)
	elif not canRotate:
		thetaPoint = 0
		rotationTimer.stop()
		canRotate = true
		
func execute(direction: int):
	canRotate = false
	thetaZero = body.rotation
	var PI2 = PI / 2
	thetaTarget = round((thetaZero + direction*PI2)/PI2) * PI2
	thetaPoint = (thetaTarget - thetaZero) / ROTATION_DURATION
	rotationTimer.reset()
