extends CanvasLayer

onready var screen_shaders = $ScreenShaders
onready var pause_menu = $PauseMenu
var is_paused := false

func _ready():
  # pause_menu.visible = false
  pass
  
func _process(_delta):
  if Input.is_action_just_pressed("pause"):
    if (is_paused):
      resume()
    else:
      pause()


func resume():
    screen_shaders.disable_pause_shader()
    # pause_menu.visible = false
    pause_menu.hide()
    is_paused = false
    get_tree().paused = false
      
func pause():
    screen_shaders.activate_pause_shader()
    # pause_menu.visible = true
    pause_menu.show()
    is_paused = true
    get_tree().paused = true
      
func _on_BackButton_pressed():
  resume()
  get_tree().change_scene("res://Assets/Screens/MainMenu.tscn")

func _on_ResumeButton2_pressed():
  if (is_paused):
    resume()
  
