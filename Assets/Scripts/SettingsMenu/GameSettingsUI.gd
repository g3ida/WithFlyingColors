extends Control

onready var vsyncCheckbox = $GridContainer/VsyncCheckbox
onready var fscreenCheckbox = $GridContainer/FullscreenCheckbox
onready var autoResolutionLabel = $GridContainer/AutoResolutionLabel
onready var resolutionSelect = $GridContainer/ResolutionUISelect

func _ready():
	vsyncCheckbox.pressed = Settings.vsync
	fscreenCheckbox.pressed = Settings.fullscreen
	toggle_auto_resolution()
	
func _on_VsyncCheckbox_toggled(button_pressed):
	Settings.vsync = button_pressed
	Event.emit_signal("Vsync_toggled", button_pressed)

func _on_FullscreenCheckbox_toggled(button_pressed):
	Settings.fullscreen = button_pressed
	Event.emit_signal("Fullscreen_toggled",button_pressed )
	toggle_auto_resolution()

func toggle_auto_resolution():
	if Settings.fullscreen:
		autoResolutionLabel.visible = true
		resolutionSelect.visible = false
	else:
		autoResolutionLabel.visible = false
		resolutionSelect.visible = true
		launch_scheduled_rescale()

func _on_UISelect_Value_changed(value):
	Settings.window_size = value

func launch_scheduled_rescale():
	var rescale_timer = Timer.new()
	rescale_timer.connect("timeout",self, "on_rescale_timeout")
	rescale_timer.wait_time = 0.4
	rescale_timer.one_shot = true
	add_child(rescale_timer, true)
	rescale_timer.start()

func on_rescale_timeout():
	Settings.window_size = resolutionSelect.selected_value