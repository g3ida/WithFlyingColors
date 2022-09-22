extends Node2D

const SAVE_FILE_PAtH = "user://save_game.save"

func save():
  var save_file = File.new()
  save_file.open(SAVE_FILE_PAtH, File.WRITE)
  var save_nodes = get_tree().get_nodes_in_group("persist")
  for node in save_nodes:
    if node.filename.empty():
      push_error("persistent node '%s' is not an instanced scene, skipped" % node.name)
      continue
    if !node.has_method("save"):
      push_error("persistent node '%s' is missing a save() function, skipped" % node.name)
      continue
    var node_data = node.call("save")
    node_data["_node_path_"] = node.get_path()
    save_file.store_line(to_json(node_data))
  save_file.close()

func load():
  var save_game = File.new()
  if not save_game.file_exists(SAVE_FILE_PAtH):
    push_error("FILE NOT FOUND")
    return # Error! We don't have a save to load.

  save_game.open(SAVE_FILE_PAtH, File.READ)
  while save_game.get_position() < save_game.get_len():
    var node_data = parse_json(save_game.get_line())
    var node_path = node_data["_node_path_"]
    var object = get_node(node_path)
    object.save_data = node_data
    object.reset()
  save_game.close()

func _ready():
  set_process(false)

func _enter_tree():
  var __ = Event.connect("checkpoint_reached", self, "_on_checkpoint")

func _exit_tree():
  Event.disconnect("checkpoint_reached", self, "_on_checkpoint")

func _on_checkpoint(_checkpoint):
  self.call_deferred("save")
