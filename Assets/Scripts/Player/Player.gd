class_name Player
extends KinematicBody2D

const PlayerRotationAction = preload("res://Assets/Scripts/Player/PlayerRotationAction.gd")
const TransoformAnimation = preload("res://Assets/Scripts/Utils/TransformAnimation.gd")
const ElasticIn = preload("res://Assets/Scripts/Utils/Interpolation/ElasticIn.gd")
const ElasticOut = preload("res://Assets/Scripts/Utils/Interpolation/ElasticOut.gd")

const SQUEEZE_ANIM_DURATION = 0.17
const SCALE_ANIM_DURATION = 0.17

# max player speed
const SPEED = 3.5 * Global.WORLD_TO_SCREEN
const SPEED_UNIT = 0.7 * Global.WORLD_TO_SCREEN
var speed_limit = SPEED
var speed_unit = SPEED_UNIT

export(Script) var StatesStore

var velocity = Vector2(0, 0)
var playerRotationAction: PlayerRotationAction

var scale_animation: TransoformAnimation
var idle_animation: TransoformAnimation
var current_animation: TransoformAnimation

var sprite_size: int
var was_on_floor: bool = true

var states_store
var player_state
var player_rotation_state

var can_dash = true

#touch input
var touch_move_input = null
var touch_jump_input = null
var touch_dash_input = null
var touch_rotation_input = null

onready var jumpParticlesNode = $JumpParticles
onready var fallTimerNode = $FallTimer

onready var faceSparatorBR_node := $FaceSeparatorBR
onready var faceSparatorBL_node := $FaceSeparatorBL
onready var faceSparatorTL_node := $FaceSeparatorTL
onready var faceSparatorTR_node := $FaceSeparatorTR

onready var bottomFaceNode := $BottomFace
onready var topFaceNode := $TopFace
onready var leftFaceNode := $LeftFace
onready var rightFaceNode := $RightFace

onready var FaceCollisionShapeL_node = $FaceCollisionShapeL
onready var FaceCollisionShapeR_node = $FaceCollisionShapeR
onready var FaceCollisionShapeT_node = $FaceCollisionShapeT
onready var FaceCollisionShapeB_node = $FaceCollisionShapeB

onready var FaceCollisionShapeTL_node = $FaceCollisionShapeTL
onready var FaceCollisionShapeTR_node = $FaceCollisionShapeTR
onready var FaceCollisionShapeBL_node = $FaceCollisionShapeBL
onready var FaceCollisionShapeBR_node = $FaceCollisionShapeBR

onready var collisionShapeNode := $CollisionShape2D
onready var animatedSpriteNode := $AnimatedSprite
onready var dashGhostTimerNode := $DashGhostTimer

onready var faceSeparatorNodes = [
  faceSparatorBR_node,
  faceSparatorBL_node,
  faceSparatorTL_node,
  faceSparatorTR_node
]

onready var faceNodes = [
  bottomFaceNode,
  topFaceNode,
  leftFaceNode,
  rightFaceNode
]

onready var faceCollisionNodes = [
  FaceCollisionShapeB_node,
  FaceCollisionShapeT_node,
  FaceCollisionShapeL_node,
  FaceCollisionShapeR_node,
]

onready var faceCornerCollisionNodes = [
  FaceCollisionShapeBR_node,
  FaceCollisionShapeBL_node,
  FaceCollisionShapeTL_node,
  FaceCollisionShapeTR_node,
]

onready var save_data = {
  "position_x": self.global_position.x,
  "position_y": self.global_position.y,
  "angle": 0.0,
  "default_corner_scale_factor": 1.0
}

#used to backup collision layer and collision mask of the player areas
var _face_separators_mask_backup = []
var _face_nodes_mask_backup = []

var current_default_corner_scale_factor = 1.0
var current_scale_factor = 1.0 # do not edit by yourself this is used by scale_corners_by

func _ready():
  playerRotationAction = PlayerRotationAction.new(self)
  animatedSpriteNode.frames.set_frame("idle", 0, Global.get_player_sprite())
  sprite_size = animatedSpriteNode.frames.get_frame("idle", 0).get_width()
  _init_sprite_animation()
  was_on_floor = is_on_floor()
  _init_faces_areas()
  _init_state()

func _init_sprite_animation():
  idle_animation = TransoformAnimation.new(0, ElasticOut.new(1, 1, 1, 0.1),0)
  scale_animation = TransoformAnimation.new(SCALE_ANIM_DURATION, ElasticOut.new(1, 1, 1, 0.1), sprite_size * 0.5)
  current_animation = idle_animation  

func _init_state():
  states_store = StatesStore.new(self)
  player_state = states_store.falling_state
  player_state._enter()
  player_rotation_state = states_store.idle_state
  player_rotation_state._enter()

func _init_faces_areas():
  for i in range(faceCollisionNodes.size()):
    for grp in faceNodes[i].get_groups():
      faceCollisionNodes[i].add_to_group(grp)
  for i in range(faceCornerCollisionNodes.size()):
    for grp in faceSeparatorNodes[i].get_groups():
      faceCornerCollisionNodes[i].add_to_group(grp)
  _fill_face_nodes_backup()
  _fill_face_separators_backup()

func _input(event):
  player_state._input(event)
  player_rotation_state._input(event)

func _physics_process(delta):
  var next_state = player_rotation_state.physics_update(delta)
  switch_rotation_state(next_state)

  var next_player_state = player_state.physics_update(delta)
  switch_state(next_player_state)

  if is_just_hit_the_floor():
    on_land()
  was_on_floor = is_on_floor()

func save():
  return save_data

func reset():
  $AnimatedSprite.play("idle")
  $AnimatedSprite.playing = false
  self.global_position = Vector2(save_data["position_x"], save_data["position_y"])
  self.velocity = Vector2.ZERO
  var angle_rot = save_data["angle"]
  self.rotate(angle_rot - self.rotation)
  self.current_default_corner_scale_factor = save_data["default_corner_scale_factor"]
  show_color_areas()
  switch_rotation_state(states_store.get_state(PlayerStatesEnum.IDLE))
  switch_state((states_store.get_state(PlayerStatesEnum.FALLING)))

func _on_checkpoint_hit(checkpoint_object: Node2D):  
  if checkpoint_object.color_group in $BottomFace.get_groups():
    save_data["angle"] = 0
  elif checkpoint_object.color_group in $LeftFace.get_groups():
    save_data["angle"] = -PI / 2
  elif checkpoint_object.color_group in $RightFace.get_groups():
    save_data["angle"] = PI / 2
  elif checkpoint_object.color_group in $TopFace.get_groups():
    save_data["angle"] = PI

  if checkpoint_object.is_inside_tree():
    save_data["position_x"] = checkpoint_object.global_position.x
    save_data["position_y"] = checkpoint_object.global_position.y

  save_data["default_corner_scale_factor"] = current_default_corner_scale_factor

func connect_signals():
  var __ = Event.connect("player_diying", self, "_on_player_diying")
  __ = Event.connect("checkpoint_reached", self, "_on_checkpoint_hit")
  __ = Event.connect("checkpoint_loaded", self, "reset")
  
func disconnect_signals():
  Event.disconnect("player_diying", self, "_on_player_diying")
  Event.disconnect("checkpoint_reached", self, "_on_checkpoint_hit")
  Event.disconnect("checkpoint_loaded", self, "reset")
      
func _enter_tree():
  Global.player = self
  connect_signals()

func _exit_tree():
  disconnect_signals()
  
func _on_player_diying(area, position, entity_type):
  var next_state = player_state._on_player_diying(area, position, entity_type)
  switch_state(next_state)

func is_just_hit_the_floor():
  return (not was_on_floor) and is_on_floor()

func on_land():
  var next_player_state = player_state.on_land()
  switch_state(next_player_state)

func switch_state(new_state):
  if (new_state != null):
    player_state._exit()
    player_state = new_state
    player_state._enter()

func switch_rotation_state(new_state):
  if (new_state != null):
    player_rotation_state._exit()
    player_rotation_state = new_state
    player_rotation_state._enter()
    
#useful for more permessiveness
func _scale_face_separators_by(factor: float) -> void:
  for face_sep in faceSeparatorNodes:
    face_sep.scale_by(factor)
    
func _scale_faces_by(factor: float) -> void:
  for face_sep in faceNodes:
    face_sep.scale_by(factor)

func scale_corners_by(factor: float) -> void:
  if current_scale_factor == factor: return  
  current_scale_factor = factor
  var edge = faceSeparatorNodes[0].edge_length
  var face = faceNodes[0].edge_length
  var total_length = 2*edge + face
  var reverse_factor = (total_length - 2 * edge * factor) / face
  _scale_face_separators_by(factor)
  _scale_faces_by(reverse_factor)

func get_collision_shape_size() -> Vector2:
  var extra_w = FaceCollisionShapeL_node.shape.extents.x
  return (collisionShapeNode.shape.extents + 2.0 * Vector2(extra_w, extra_w)) * 2.0

func contains_node(node) -> bool:
  return node in get_children()

#this function is a hack for bullets and fast monving objects because of this godot issue:
#https://github.com/godotengine/godot/issues/43743
func on_fast_area_colliding_with_player_shape(body_shape_index, color_area: Area2D, entity_type):
  var collision_shape: CollisionShape2D =  self.shape_owner_get_owner(body_shape_index)
  var shape_groups = collision_shape.get_groups()
  var group_found = false
  for group in shape_groups:
    if color_area.is_in_group(group):
      group_found = true
  if not group_found:
    Event.emit_signal("player_diying", color_area, global_position, entity_type) 

# Face areas backup
func _fill_face_nodes_backup():
  _face_nodes_mask_backup = []
  for face in faceNodes:
    _face_nodes_mask_backup.append({"layer": face.collision_layer, "mask": face.collision_mask})

func _fill_face_separators_backup():
  _face_separators_mask_backup = []
  for face in faceSeparatorNodes:
    _face_separators_mask_backup.append({"layer": face.collision_layer, "mask": face.collision_mask})

func hide_color_areas():
  _fill_face_separators_backup()
  for face in faceSeparatorNodes:
    face.collision_layer = 0; face.collision_mask = 0
  _fill_face_nodes_backup()
  for face in faceNodes:
    face.collision_layer = 0; face.collision_mask = 0

func set_collision_shapes_disabled_flag_deferred(disable: bool):
  call_deferred("_set_collision_shapes_disabled_flag", disable)

func _set_collision_shapes_disabled_flag(disable: bool):
  for face in faceCollisionNodes:
    face.disabled = disable
  for face in faceCornerCollisionNodes:
    face.disabled = disable
  collisionShapeNode.disabled = disable

func show_color_areas():
  for i in range(faceSeparatorNodes.size()):
    faceSeparatorNodes[i].collision_layer = _face_separators_mask_backup[i]["layer"]
    faceSeparatorNodes[i].collision_mask = _face_separators_mask_backup[i]["mask"]
  for i in range(faceNodes.size()):
    faceNodes[i].collision_layer = _face_nodes_mask_backup[i]["layer"]
    faceNodes[i].collision_mask = _face_nodes_mask_backup[i]["mask"]
