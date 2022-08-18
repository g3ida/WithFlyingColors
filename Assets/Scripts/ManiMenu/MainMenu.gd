extends Control

const DURATION = 0.35
const DISTANCE = 800.0
const DELAY = 0.3

var animators = []

func init_label_animator(el, delay: float) -> Animator:
	var start = el.rect_position.x - DISTANCE
	var end = el.rect_position.x
	var interpolation = PowInterpolation.new(2)
	var duration = DURATION
	return Animator.new(start, end, funcref(el, "update_label_position_x"), duration, delay, interpolation)

func init_box_animator(el, delay: float) -> Animator:
	var start = el.rect_position.y + DISTANCE
	var end = el.rect_position.y
	var interpolation = PowInterpolation.new(2)
	var duration = DURATION
	return Animator.new(start, end, funcref(el, "update_position_y"), duration, delay, interpolation)


func _ready():
	animators.append(init_label_animator($WITH, 2*DELAY))
	animators.append(init_label_animator($FLYING, 3*DELAY))
	animators.append(init_label_animator($COLORS, 4*DELAY))
	animators.append(init_box_animator($MenuBox, 3*DELAY))

func _on_quit():
	for animator in animators:
		animator.reset()
		animator.reverse()

func _physics_process(delta):
	for animator in animators:
		animator.update(delta)

