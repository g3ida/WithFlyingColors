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

var reset_position: Vector2
var reset_angle: float = 0

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

var faceSeparatorNodes = []
var faceNodes = []

var saved_default_corner_scale_factor = 1.0
var current_default_corner_scale_factor = 1.0
var current_scale_factor = 1.0 # do not edit by yourself this is used by scale_corners_by

func _ready():
  playerRotationAction = PlayerRotationAction.new(self)
  sprite_size = $AnimatedSprite.frames.get_frame("idle", 0).get_width()
  _init_sprite_animation()
  was_on_floor = is_on_floor()
  self.reset_position = self.global_position
  _init_faces_areas()
  _init_state()

func _init_sprite_animation():
  idle_animation = TransoformAnimation.new(
    0.0,
    ElasticOut.new(1.0, 1.0, 1, 0.1),
    0)
  scale_animation = TransoformAnimation.new(
    SCALE_ANIM_DURATION,
    ElasticOut.new(1.0, 1.0, 1, 0.1),
    sprite_size * 0.5)
  current_animation = idle_animation  

func _init_state():
  states_store = StatesStore.new(self)
  player_state = states_store.falling_state
  player_state._enter()
  player_rotation_state = states_store.idle_state
  player_rotation_state._enter()

func _init_faces_areas():
  faceSeparatorNodes = [
    faceSparatorBR_node,
    faceSparatorBL_node,
    faceSparatorTL_node,
    faceSparatorTR_node
  ]
  faceNodes = [
    bottomFaceNode,
    topFaceNode,
    leftFaceNode,
    rightFaceNode
  ]
  FaceCollisionShapeL_node.add_to_group(leftFaceNode.get_groups()[0])
  FaceCollisionShapeR_node.add_to_group(rightFaceNode.get_groups()[0])
  FaceCollisionShapeT_node.add_to_group(topFaceNode.get_groups()[0])
  FaceCollisionShapeB_node.add_to_group(bottomFaceNode.get_groups()[0])

  for group in faceSparatorBR_node.get_groups():
    FaceCollisionShapeBR_node.add_to_group(group)
  for group in faceSparatorTR_node.get_groups():
    FaceCollisionShapeTR_node.add_to_group(group)
  for group in faceSparatorBL_node.get_groups():
    FaceCollisionShapeBL_node.add_to_group(group)
  for group in faceSparatorTL_node.get_groups():
    FaceCollisionShapeTL_node.add_to_group(group)

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

func reset():
  $AnimatedSprite.play("idle")
  $AnimatedSprite.playing = false
  self.global_position = reset_position
  switch_rotation_state(states_store.get_state(PlayerStatesEnum.IDLE))
  switch_state((states_store.get_state(PlayerStatesEnum.FALLING)))
  self.rotate(self.reset_angle - self.rotation)
  
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

func _on_checkpoint_hit(checkpoint_object: Node2D):
  if checkpoint_object.is_inside_tree():
    self.reset_position = checkpoint_object.global_position
  self.saved_default_corner_scale_factor = current_default_corner_scale_factor
  
  if checkpoint_object.color_group in $BottomFace.get_groups():
    self.reset_angle = 0
  elif checkpoint_object.color_group in $LeftFace.get_groups():
    self.reset_angle = -PI / 2
  elif checkpoint_object.color_group in $RightFace.get_groups():
    self.reset_angle = PI / 2
  elif checkpoint_object.color_group in $TopFace.get_groups():
    self.reset_angle = PI

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
