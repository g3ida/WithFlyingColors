extends Node2D

export(String) var track = null
  
onready var CutsceneNode = $Cutscene

func _enter_tree():
  if track != null:
    AudioManager.music_track_manager.load_track(track)
    AudioManager.music_track_manager.play_track(track)

func _exit_tree():
  AudioManager.music_track_manager.stop()
  
func _ready():
  set_process(false)
  Global.cutscene = CutsceneNode
