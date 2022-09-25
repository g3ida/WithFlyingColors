extends Node2D
  
func _enter_tree():
  AudioManager.music_track_manager.load_track("level1")
  AudioManager.music_track_manager.play_track("level1")

func _exit_tree():
  AudioManager.music_track_manager.stop()
  
func _ready():
  set_process(false)
