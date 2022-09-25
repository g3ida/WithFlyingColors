extends Node2D

const SAVE_FILE_PATH = "user://save_slot_%s.save"
const SAVE_IMAGE_PATH = "user://save_slot_img_%s.png"

const SAVE_SLOTS = [
  SAVE_FILE_PATH % "1",
  SAVE_FILE_PATH % "2",
  SAVE_FILE_PATH % "3"
]

const SAVE_IMAGE_SLOTS = [
  SAVE_IMAGE_PATH % "1",
  SAVE_IMAGE_PATH % "2",
  SAVE_IMAGE_PATH % "3"
]

onready var is_slot_filled_array = []
onready var slot_meta_data = []
onready var has_filled_slots = false

var current_slot_index = 0

# Special variables (always make them start and end with '_' to avoid potential conflicts)
const NODE_PATH_VAR = "_node_path_"
const SAVE_DATE_VAR = "_save_date_"

func _generate_save_meta_data(save_slot_index):
  return {
    "image_path": get_save_slot_image_path(save_slot_index),
    "save_time": Time.get_unix_time_from_datetime_dict(Time.get_datetime_dict_from_system()),
    "scene_path": get_tree().current_scene.get_child(0).filename
  }

func save(save_slot_index):
  var file_path = get_save_slot_file_path(save_slot_index)
  var save_file = File.new()
  save_file.open(file_path, File.WRITE)
  #save meta data
  var save_meta_data = _generate_save_meta_data(save_slot_index)
  save_screenshot(save_meta_data["image_path"])
  save_file.store_line(to_json(save_meta_data))
  #save nodes
  var save_nodes = get_tree().get_nodes_in_group("persist")
  for node in save_nodes:
    if node.filename.empty():
      push_error("persistent node '%s' is not an instanced scene, skipped" % node.name)
      continue
    if !node.has_method("save"):
      push_error("persistent node '%s' is missing a save() function, skipped" % node.name)
      continue
    var node_data = node.call("save")
    node_data[NODE_PATH_VAR] = node.get_path()
    save_file.store_line(to_json(node_data))
  current_slot_index = save_slot_index
  save_file.close()
  
func load(save_slot_index):
  var file_path = get_save_slot_file_path(save_slot_index)
  var save_game = File.new()
  if not is_slot_filled(file_path):
    push_error("FILE NOT FOUND")
    return # Error! We don't have a save to load.
  save_game.open(file_path, File.READ)
  while save_game.get_position() < save_game.get_len():
    var node_data = parse_json(save_game.get_line())
    var node_path = node_data[NODE_PATH_VAR]
    var object = get_node(node_path)
    object.save_data = node_data
    object.reset()
  current_slot_index = save_slot_index
  save_game.close()

func _ready():
  _init_check_filled_slots()
  _init_save_slot_meta_data()
  #remove_all_save_slots()
  set_process(false)

func _enter_tree():
  var __ = Event.connect("checkpoint_reached", self, "_on_checkpoint")

func _exit_tree():
  Event.disconnect("checkpoint_reached", self, "_on_checkpoint")

func _on_checkpoint(_checkpoint):
  self.call_deferred("save_to_current_slot")

func _init_check_filled_slots():
  var file = File.new()
  for slot_path in SAVE_SLOTS:
    var exists = file.file_exists(slot_path)
    has_filled_slots = exists or has_filled_slots
    is_slot_filled_array.append(exists)

func get_save_slot_file_path(save_slot_index):
  return SAVE_SLOTS[save_slot_index]

func get_save_slot_image_path(save_slot_index):
  return SAVE_IMAGE_SLOTS[save_slot_index]

func is_slot_filled(save_slot_index):
  return is_slot_filled_array[save_slot_index]

func _init_save_slot_meta_data():
  for slot_idx in range(is_slot_filled_array.size()):
    if is_slot_filled(slot_idx):
      var save_file = File.new()
      save_file.open(get_save_slot_file_path(slot_idx), File.READ)
      var meta_data = parse_json(save_file.get_line())
      slot_meta_data.append(meta_data)
      save_file.close()
    else:
      slot_meta_data.append(null)

func get_slot_meta_data(save_slot_index):
  return slot_meta_data[save_slot_index]

func get_current_slot_meta_data():
  return get_slot_meta_data(current_slot_index)

func save_to_current_slot():
  save(current_slot_index)
  
func load_if_needed() -> bool:
  if is_slot_filled(current_slot_index):
    var __ = load(current_slot_index)
    return true
  return false

func remove_save_slot(save_slot_index):
  if is_slot_filled(save_slot_index):
    var dir = Directory.new()
    dir.remove(get_save_slot_file_path(save_slot_index))
    dir.remove(get_save_slot_image_path(save_slot_index))
    
func remove_all_save_slots():
  for slot_idx in range(is_slot_filled_array.size()):
    remove_save_slot(slot_idx)

func save_screenshot(file_path):
  var image = get_viewport().get_texture().get_data()
  image.flip_y()
  image.shrink_x2()
  image.save_png(file_path)

func load_image(file_path) -> ImageTexture:
  var file = File.new()
  if file.file_exists(file_path):
    var texture = ImageTexture.new()
    var image = Image.new()
    image.load(file_path)
    texture.create_from_image(image)
    return texture
  return null

func load_slot_image(save_slot_index) -> ImageTexture:
  if is_slot_filled(save_slot_index):
    return load_image(get_save_slot_image_path(save_slot_index))
  return null

func get_most_recent_saved_slot_index():
  var max_slot_index = 0
  var max_slot_time = -1 
  for slot_idx in range(is_slot_filled_array.size()):
    if is_slot_filled(slot_idx):
      if max_slot_time < slot_meta_data[slot_idx]["save_time"]:
        max_slot_time = slot_meta_data[slot_idx]["save_time"]
        max_slot_index = slot_idx
  return max_slot_index
