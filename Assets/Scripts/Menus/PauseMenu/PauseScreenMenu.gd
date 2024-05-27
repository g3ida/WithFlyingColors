extends CanvasLayer

onready var screen_shaders = $ScreenShaders
onready var pause_menu = $PauseMenu
var is_paused := false

func _ready():
  pass
  
func _process(_delta):
  if Input.is_action_just_pressed("pause"):
    if (is_paused):
      resume()
    else:
      pause()

func resume():
    AudioManager.resume_all_sfx()
    AudioManager.music_track_manager.SetPauseMenuEffect(false)
    screen_shaders.disable_pause_shader()
    pause_menu.hide()
    is_paused = false
    get_tree().paused = false
    Event.emit_signal("pause_menu_exit")
      
func pause():
  AudioManager.pause_all_sfx() 
  AudioManager.music_track_manager.SetPauseMenuEffect(true)
  screen_shaders.activate_pause_shader()
  pause_menu.show()
  is_paused = true
  get_tree().paused = true
  Event.emit_signal("pause_menu_enter")
  
func _on_BackButton_pressed():
  AudioManager.stop_all_sfx()
  resume()
  pause_menu.go_to_main_menu()

func _on_ResumeButton2_pressed():
  if (is_paused):
    resume()

func _on_LevelSelectButton_pressed():
  AudioManager.stop_all_sfx()
  resume()
  pause_menu.go_to_level_select_menu()
