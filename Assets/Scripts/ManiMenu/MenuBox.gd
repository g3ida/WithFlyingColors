extends Control

var PlayerRotationAction = preload("res://Assets/Scripts/Player/PlayerRotationAction.gd")
var box_rotation: PlayerRotationAction
var buttons = []
var active_index = 0

func _ready():
	box_rotation = PlayerRotationAction.new($MenuBox)
	buttons = [
		$MenuBox/Sprite/PlayButton,
		$MenuBox/Sprite/SettingsButton,
		$MenuBox/Sprite/StatsButton,
		$MenuBox/Sprite/QuitButton
		]
		
	for b in buttons:
		b.disabled = true
	buttons[active_index].disabled = false

func update_position_y(y: float):
	self.rect_position.y = y

func _physics_process(delta):
	box_rotation.step(delta)
	if Input.is_action_just_pressed("rotate_left") or Input.is_action_just_pressed("ui_left"):
		_on_LeftButton_pressed()
	elif Input.is_action_just_pressed("rotate_right") or Input.is_action_just_pressed("ui_right"):
		_on_RightButton_pressed()
	elif Input.is_action_just_pressed("ui_accept"):
		pass #move 2 screen


func _on_RightButton_pressed():
	buttons[active_index].disabled = true
	active_index = (active_index - 1) % buttons.size()
	buttons[active_index].disabled = false
	box_rotation.execute(1)


func _on_LeftButton_pressed():
	buttons[active_index].disabled = true
	active_index = (active_index + 1) % buttons.size()
	buttons[active_index].disabled = false
	box_rotation.execute(-1)


func _on_PlayButton_pressed():
	Event.emit_signal("Play_button_pressed")


func _on_QuitButton_pressed():
	Event.emit_signal("Quit_button_pressed")


func _on_SettingsButton_pressed():
	Event.emit_signal("Settings_button_pressed")


func _on_StatsButton_pressed():
	Event.emit_signal("Stats_button_pressed")
