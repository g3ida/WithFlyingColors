extends Node2D

export(String) var track = null
  
onready var CutsceneNode = $Cutscene

func _enter_tree():
  if track != null:
    AudioManager.music_track_manager.LoadTrack(track)
    AudioManager.music_track_manager.PlayTrack(track)

func _exit_tree():
  AudioManager.music_track_manager.Stop()
  
func _ready():
  set_process(false)
  Global.cutscene = CutsceneNode
