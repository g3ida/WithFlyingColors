extends Node2D

func connect_signals():
  var __ = Event.connect("player_died", self, "_on_game_over")

func disconnect_signals():
  Event.disconnect("player_died", self, "_on_game_over")
  
func _enter_tree():
  connect_signals()

func _exit_tree():
  disconnect_signals()
  AudioManager.music_track_manager.stop()

func _on_game_over():
  Event.emit_signal("checkpoint_loaded")

func _ready():
  set_process(false)
  var meta_data = SaveGame.get_current_slot_meta_data()
  var scene_ressource = null
  var is_new_game = (meta_data == null)
  if is_new_game:
    scene_ressource = load(MenuManager.START_LEVEL_MENU_SCENE)
  else:
    scene_ressource = load(meta_data["scene_path"])
  var scene_instance = scene_ressource.instance()
  add_child(scene_instance)
  scene_instance.set_owner(self)
  SaveGame.call_deferred("load_if_needed")
