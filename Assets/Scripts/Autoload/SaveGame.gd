extends Node2D

const SAVE_FILE_PATH = "user://save_slot_%s.save"
const SAVE_IMAGE_PATH = "user://save_slot_img_%s.png"
const SAVE_INFO_PATH = "user://save_info.save"

const NUM_SLOTS = 3

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
onready var slot_last_load_date = []

var current_slot_index = 0 #index of current slot

# Special variables (always make them start and end with '_' to avoid potential conflicts)
const NODE_PATH_VAR = "_node_path_"
const SAVE_DATE_VAR = "_save_date_"

func _get_unix_timestamp():
  return Time.get_unix_time_from_datetime_dict(Time.get_datetime_dict_from_system())

func _generate_save_meta_data(save_slot_index):
  return {
    "image_path": get_save_slot_image_path(save_slot_index),
    "save_time": _get_unix_timestamp(),
    "scene_path": get_tree().current_scene.get_child(0).filename,
    "progress": 0 #todo: calculate progress
  }
  
func save(save_slot_index, new_empty_slot = false):
  var file_path = get_save_slot_file_path(save_slot_index)
  var save_file = File.new()
  save_file.open(file_path, File.WRITE)
  #save meta data
  var save_meta_data = _generate_save_meta_data(save_slot_index)
  save_screenshot(save_meta_data["image_path"])
  save_file.store_line(to_json(save_meta_data))
  #save nodes
  if not new_empty_slot:
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
  _update_slot_load_date(save_slot_index)
  
func load_level(save_slot_index):
  var file_path = get_save_slot_file_path(save_slot_index)
  var save_game = File.new()
  if not is_slot_filled(save_slot_index):
    push_error("FILE NOT FOUND")
    return # Error! We don't have a save to load.
  save_game.open(file_path, File.READ)
  var _meta_data = parse_json(save_game.get_line()) #the metadata is ignored
  while save_game.get_position() < save_game.get_len():
    var node_data = parse_json(save_game.get_line())
    var node_path = node_data[NODE_PATH_VAR]
    var object = get_node(node_path)
    object.save_data = node_data
    object.reset()
  current_slot_index = save_slot_index
  save_game.close()
  _update_slot_load_date(save_slot_index)  #update last load date needed for the continue button
  # Update camera position to the player position avoiding smoothing
  # which would make you see the camera move quickly to the checkpoint position
  # when we load a level. We put it here instead of the reset method because
  # I like the smoothing effect when the player looses
  Global.camera.update_position(Global.player.global_position)

func _ready():
  init()
  
func init(create_slot_if_emty = true):
  refresh()
  current_slot_index = get_most_recently_loaded_slot_index()
  #remove_all_save_slots() #uncomment to reset the game then comment it back
  set_process(false)
  # in case no slot is filled (first time launching the game) create a slot. 
  if create_slot_if_emty and not has_filled_slots:
    current_slot_index = 0
    SaveGame.save(current_slot_index, true)
    init()

func refresh():
  _init_check_filled_slots()
  _init_save_slot_meta_data()
  _init_slot_last_load_date()

func _enter_tree():
  var __ = Event.connect("checkpoint_reached", self, "_on_checkpoint")

func _exit_tree():
  Event.disconnect("checkpoint_reached", self, "_on_checkpoint")

func _on_checkpoint(_checkpoint):
  self.call_deferred("save_to_current_slot")

func _init_check_filled_slots():
  is_slot_filled_array = []
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

func does_slot_have_progress(save_slot_index):
  return is_slot_filled(save_slot_index) \
    and get_slot_meta_data(save_slot_index)["progress"] > Global.EPSILON

func _init_save_slot_meta_data():
  slot_meta_data = []
  for slot_idx in range(NUM_SLOTS):
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
    var __ = load_level(current_slot_index)
    return true
  return false

func remove_save_slot(save_slot_index):
  if is_slot_filled(save_slot_index):
    var dir = Directory.new()
    dir.remove(get_save_slot_file_path(save_slot_index))
    dir.remove(get_save_slot_image_path(save_slot_index))
    # reset currently selected slot
    if current_slot_index == save_slot_index:
      current_slot_index = -1
    
func remove_all_save_slots():
  for slot_idx in range(is_slot_filled_array.size()):
    remove_save_slot(slot_idx)

func save_screenshot(file_path):
  var image = get_viewport().get_texture().get_data()
  image.flip_y()
  image.shrink_x2() #todo: resize image to fixed size
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

func get_most_recently_loaded_slot_index():
  var max_slot_index = 0
  var max_slot_time = -1 
  for slot_idx in range(NUM_SLOTS):
    if is_slot_filled(slot_idx):
      if max_slot_time < slot_last_load_date[slot_idx]:
        max_slot_time = slot_last_load_date[slot_idx]
        max_slot_index = slot_idx
  return max_slot_index 

func get_most_recent_saved_slot_index():
  var max_slot_index = 0
  var max_slot_time = -1 
  for slot_idx in range(NUM_SLOTS):
    if is_slot_filled(slot_idx):
      if max_slot_time < slot_meta_data[slot_idx]["save_time"]:
        max_slot_time = slot_meta_data[slot_idx]["save_time"]
        max_slot_index = slot_idx
  return max_slot_index

func _save_game_info():
  var data = {
    "slot_last_load_date": slot_last_load_date,
  }
  var save_file = File.new()
  save_file.open(SAVE_INFO_PATH, File.WRITE)
  save_file.store_line(to_json(data))
  save_file.close()

func _load_game_info():
  var load_file = File.new()
  if load_file.file_exists(SAVE_INFO_PATH):
    load_file.open(SAVE_INFO_PATH, File.READ)
    var data = parse_json(load_file.get_line())
    slot_last_load_date = data["slot_last_load_date"]
    load_file.close()
    return true
  return false

func _init_slot_last_load_date():
  if not _load_game_info():
    slot_last_load_date = []
    for _slot_idx in range(NUM_SLOTS):
      slot_last_load_date.append(null)

func _update_slot_load_date(slot_index: int):
  slot_last_load_date[slot_index] = _get_unix_timestamp()
  _save_game_info()
