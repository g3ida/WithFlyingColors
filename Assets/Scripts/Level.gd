extends Node2D

export var level_path: String

func connect_signals():
	Event.connect("player_died", self, "_on_game_over")

func disconnect_signals():
	Event.disconnect("player_died", self, "_on_game_over")
	
func _enter_tree():
	connect_signals()

func _exit_tree():
	disconnect_signals()

func _on_game_over():
	go_to_checkpoint()

func go_to_checkpoint() -> void:
	Global.player.reset()
	#get_tree().change_scene(self.level_path)
