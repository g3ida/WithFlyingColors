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


func connect_signals():
	Event.connect("Play_button_pressed", self, "_on_Play_button_pressed")
	Event.connect("Quit_button_pressed", self, "_on_Quit_button_pressed")
	Event.connect("Settings_button_pressed", self, "_on_Settings_button_pressed")
	Event.connect("Stats_button_pressed", self, "_on_Stats_button_pressed")

func disconnect_signals():
	Event.disconnect("Play_button_pressed", self, "_on_Play_button_pressed")
	Event.disconnect("Quit_button_pressed", self, "_on_Quit_button_pressed")
	Event.disconnect("Settings_button_pressed", self, "_on_Settings_button_pressed")
	Event.disconnect("Stats_button_pressed", self, "_on_Stats_button_pressed")
					
func _enter_tree():
	connect_signals()
		
func _exit_tree():
	disconnect_signals()

func _on_Play_button_pressed():
	get_tree().change_scene("res://Levels/Level1.tscn")

func _on_Quit_button_pressed():
	get_tree().quit()

func _on_Settings_button_pressed():
	get_tree().change_scene("res://Assets/Entities/SettingsMenu/SettingsMenu.tscn")
	
func _on_Stats_button_pressed():
	get_tree().change_scene("res://Assets/Entities/StatsMenu/StatsMenu.tscn")
