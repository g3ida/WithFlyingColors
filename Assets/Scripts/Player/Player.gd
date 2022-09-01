class_name Player
extends KinematicBody2D

const PlayerRotationAction = preload("res://Assets/Scripts/Player/PlayerRotationAction.gd")
const TransoformAnimation = preload("res://Assets/Scripts/Utils/TransformAnimation.gd")
const ElasticIn = preload("res://Assets/Scripts/Utils/Interpolation/ElasticIn.gd")
const ElasticOut = preload("res://Assets/Scripts/Utils/Interpolation/ElasticOut.gd")

const SQUEEZE_ANIM_DURATION = 0.17
const SCALE_ANIM_DURATION = 0.17

var velocity = Vector2(0, 0)
var playerRotationAction: PlayerRotationAction

var scale_animation: TransoformAnimation
var idle_animation: TransoformAnimation
var current_animation: TransoformAnimation

var sprite_size: int
var was_on_floor: bool = true

var reset_position: Vector2
var reset_angle: float = 0

var states_store: PlayerStatesStore
var player_state
var player_rotation_state

var can_dash = true

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

var faceSeparatorNodes = []
var faceNodes = []

func _ready():
  playerRotationAction = PlayerRotationAction.new(self)
  sprite_size = $AnimatedSprite.frames.get_frame("idle", 0).get_width()
  
  idle_animation = TransoformAnimation.new(
    0.0,
    ElasticOut.new(1.0, 1.0, 1, 0.1),
    0)
  scale_animation = TransoformAnimation.new(
    SCALE_ANIM_DURATION,
    ElasticOut.new(1.0, 1.0, 1, 0.1),
    sprite_size * 0.5)
    
  current_animation = idle_animation
  was_on_floor = is_on_floor()
  self.reset_position = self.global_position
  
  states_store = PlayerStatesStore.new(self)
  player_state = states_store.falling_state
  player_state.enter()
  player_rotation_state = states_store.idle_state
  player_rotation_state.enter()
  
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
  self.reset_position = checkpoint_object.global_position
  
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
    player_state.exit()
    player_state = new_state
    player_state.enter()

func switch_rotation_state(new_state):
  if (new_state != null):
    player_rotation_state.exit()
    player_rotation_state = new_state
    player_rotation_state.enter()
    
#useful for more permessiveness
func scale_face_separators_by(factor: float) -> void:
  for face_sep in faceSeparatorNodes:
    face_sep.scale_by(factor)
    
func scale_faces_by(factor: float) -> void:
  for face_sep in faceNodes:
    face_sep.scale_by(factor)
