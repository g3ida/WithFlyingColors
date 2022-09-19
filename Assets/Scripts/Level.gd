extends Node2D

export var level_path: String

func connect_signals():
  var __ = Event.connect("player_died", self, "_on_game_over")

func disconnect_signals():
  Event.disconnect("player_died", self, "_on_game_over")
  
func _enter_tree():
  connect_signals()
  AudioManager.music_track_manager.load_track("level1")
  AudioManager.music_track_manager.play_track("level1")

func _exit_tree():
  disconnect_signals()
  AudioManager.music_track_manager.stop()

func _on_game_over():
  Event.emit_signal("checkpoint_loaded")
