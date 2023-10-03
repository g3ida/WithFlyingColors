extends Node2D

func connect_signals():
  var __ = Event.connect("player_died", self, "_on_game_over")
  __ = Event.connect("level_cleared", self, "_on_level_cleared")

func disconnect_signals():
  Event.disconnect("player_died", self, "_on_game_over")
  Event.disconnect("level_cleared", self, "_on_level_cleared")
  
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
  var is_new_game = (meta_data == null) or (meta_data["progress"] <= 0.0)
  if not MenuManager.level_scene_path.empty():
    scene_ressource = load(MenuManager.level_scene_path)
    _setup_scene_game(scene_ressource, false)
  elif is_new_game:
    # fixme: need to load the first level dynamically
    scene_ressource = load(MenuManager.START_LEVEL_MENU_SCENE)
    _setup_scene_game(scene_ressource, true)
  else: # load case
    scene_ressource = load(meta_data["scene_path"])
    _setup_scene_game(scene_ressource, true)

func _setup_scene_game(scene_ressource, try_load):
  var scene_instance = scene_ressource.instance()
  add_child(scene_instance)
  scene_instance.set_owner(self)
  if try_load:
    SaveGame.call_deferred("load_if_needed")

func _on_level_cleared():
  Global.pause_menu.navigate_to_screen(MenuManager.Menus.LEVEL_CLEAR_MENU)
