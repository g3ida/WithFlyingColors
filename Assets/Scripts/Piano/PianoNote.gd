extends KinematicBody2D

enum NoteStates {
  RELEASED,
  PRESSING,
  PRESSED,
  RELEASING,
}

signal on_note_pressed(note)
signal on_note_released(note)

const PairTexture = preload("res://Assets/Sprites/Piano/note_1.png")
const OddTexture = preload("res://Assets/Sprites/Piano/note_2.png")

const NoteEdgeTextures = [
  preload("res://Assets/Sprites/Piano/note_edge.png"),
  preload("res://Assets/Sprites/Piano/note_edge2.png"),
  preload("res://Assets/Sprites/Piano/note_edge3.png"),
]
const PRESS_OFFSET = Vector2(0, 25)
const PRESS_SPEED = 2.5 * Global.WORLD_TO_SCREEN
const RAYCAST_Y_OFFSET = 3.0
const RAYCAST_LENGTH = 20.0
const RESPONSIVENESS = 0.06

export var index = 0
export(String) var color_group setget set_color_group, get_color_group
# a numn int from 0 to 5
export var note_edge_index = 0 setget set_note_edge_index, get_note_edge_index

onready var SpriteNode = $Sprite
onready var TweenNode = $Tween
onready var AreaCollisionShapeNode = $Area2D/CollisionShape2D
onready var CollisionShapeNode = $CollisionShape2D
onready var ResponsivenessTimerNode = $ResponsivenessTimer

onready var released_position = position
onready var calculated_position = position

var current_state = NoteStates.RELEASED

func _set_texture():
  if index % 2 == 0:
    SpriteNode.texture = PairTexture
  else:
    SpriteNode.texture = OddTexture

func _setup_responsiveness_timer():
  ResponsivenessTimerNode.autostart = false
  ResponsivenessTimerNode.wait_time = RESPONSIVENESS

func _ready():
  _setup_responsiveness_timer()
  _set_texture()

func _physics_process(_delta):
  self.position = calculated_position
  _start_releasing_note_timer_if_relevant()

func _move_to_position(dest_position):
  TweenNode.remove_all()
  var duration = abs(calculated_position.y - dest_position.y) / PRESS_SPEED
  TweenNode.interpolate_property(
    self,
    "calculated_position",
    calculated_position,
    dest_position,
    duration,
    Tween.TRANS_LINEAR,
    Tween.EASE_IN_OUT,
    0)
  TweenNode.start()

func _is_releasing_or_released_state():
  return current_state == NoteStates.RELEASED or current_state == NoteStates.RELEASING
  
func _is_pressing_or_pressed_state():
  return current_state == NoteStates.PRESSED or current_state == NoteStates.PRESSING

func _on_Area2D_body_entered(body):
  if body == Global.player:
    _press_note_if_relevant()

func _press_note_if_relevant():
  if _is_releasing_or_released_state():
    _stop_timer_if_relevant()
    _press_note()

func _press_note():
  current_state = NoteStates.PRESSING
  _move_to_position(released_position + PRESS_OFFSET)

func _on_Area2D_body_exited(body):
  if body == Global.player:
    _start_releasing_note_timer_if_relevant()

func _start_releasing_note_timer_if_relevant():
  if _is_pressing_or_pressed_state() and !_check_if_player_is_above_the_note():
    _start_timer_if_stopped()

func _relese_note_if_relevant():
  if _is_pressing_or_pressed_state() and !_check_if_player_is_above_the_note():
    _release_note()

func _release_note():
  current_state = NoteStates.RELEASING
  _move_to_position(released_position)

func _on_Tween_tween_completed(_object, _key):
  if current_state == NoteStates.PRESSING:
    current_state = NoteStates.PRESSED
    emit_signal("on_note_pressed", self)
  elif current_state == NoteStates.RELEASING:
    current_state = NoteStates.RELEASED
    emit_signal("on_note_released", self)

func _get_detection_area_shape_size():
  return AreaCollisionShapeNode.shape.extents * 2.0

func _get_collision_shape_size():
  return CollisionShapeNode.shape.extents * 2.0

func _get_ray_lines_in_global_position():
  var rays = []
  var note_half_size = _get_detection_area_shape_size() * 0.5 * self.scale
  var from_offset_x = [
    -note_half_size.x,
    -note_half_size.x * 0.5,
    0.0,
    note_half_size.x * 0.5,
    note_half_size.x
  ]
  for offset in from_offset_x:
    var from := global_position + Vector2(offset, -_get_collision_shape_size().y*0.5+RAYCAST_Y_OFFSET)
    var to := from + Vector2(0.0, -RAYCAST_LENGTH)
    rays.append({"from": from, "to": to})
  return rays

func _raycast_player():
  var space_state = get_world_2d().direct_space_state
  var rays = _get_ray_lines_in_global_position()
  for ray in rays:
    var from = ray["from"]
    var to = ray["to"]
    var result = space_state.intersect_ray(from, to, [self])
    if (!result.empty()):
      if result["collider"] == Global.player:
        return true
  return false

func _is_player_standing_or_falling():
  var player = Global.player
  var player_state = player.player_state.base_state
  return player_state != PlayerStatesEnum.JUMPING and player.velocity.y >= -Constants.EPSILON

func _check_if_player_is_above_the_note():
  return _raycast_player() and _is_player_standing_or_falling()

# Uncomment this code to debug draw raycast rays
# func _draw():
#   var rays = _get_ray_lines_in_global_position()
#   for ray in rays:
#     var from = ray["from"] - global_position
#     var to = ray["to"] - global_position
#     draw_line(from, to, Color.red, 2.0)

func _start_timer_if_stopped():
  if ResponsivenessTimerNode.is_stopped():
    ResponsivenessTimerNode.autostart = true
    ResponsivenessTimerNode.start()

func _stop_timer_if_relevant():
  if !ResponsivenessTimerNode.is_stopped():
    ResponsivenessTimerNode.autostart = false
    ResponsivenessTimerNode.stop()

func _on_ResponsivenessTimer_timeout():
  _relese_note_if_relevant()
  _stop_timer_if_relevant()

func set_color_group(_color_group: String):
  color_group = _color_group
  $NoteEdge.modulate = ColorUtils.get_color(_color_group)
  var area = $ColorArea
  for grp in area.get_groups():
    area.remove_from_group(grp)
  area.add_to_group(_color_group)

func get_color_group():
  return color_group

func set_note_edge_index(note_index: int):
  var scale = -1 if(int((note_index / (NoteEdgeTextures.size()+1.0))) % 2 == 0) else 1
  note_edge_index = note_index % NoteEdgeTextures.size()
  $NoteEdge.texture = NoteEdgeTextures[note_edge_index]
  $NoteEdge.scale = Vector2(scale, 1)
  
func get_note_edge_index():
  return note_edge_index
