extends Node2D

const TempleGemScene = preload("res://Assets/Scenes/Temple/TempleGem.tscn")

const WAIT_DELAY = 0.1
const MAX_GEM_TEMPLE_ANGULAR_VELOCITY = 10.0*PI
const DURATION_TO_FULL_GEM_ROTATION_SPEED = 2.7
const ROTATION_DURATION = 3.6
const BLOOM_SPRITE_MAX_SCALE = 25.0
const BLOOM_SPRITE_SCALE_DURATION = 1.2
const GEMS_EASE_TYPE = [1, 1, 0, 0]

enum States {
  NOT_TRIGGERED,
  WALK_PHASE,
  COLLECT_PHASE,
  ROTATION_PHASE,
  BLOOM_PHASE,
  END_PHASE,
}

var temple_gems = []
var gems_angular_velocity = 0
var num_active_gems = 0
var bloom_sprite_scale = 1.0
var current_state = States.NOT_TRIGGERED

onready var GemSlotsContainerNode = $GemsContainer
onready var GemsSlotsNodes = GemSlotsContainerNode.get_children()
onready var StartGemsAreaNode = $StartGemsArea
onready var RotationTimerNode = $RotationTimer
onready var BloomSpriteNode = $BloomSprite

var tweener: SceneTreeTween

func _ready():
  BloomSpriteNode.visible = false

func _on_TriggerArea_body_entered(body):
  if current_state == States.NOT_TRIGGERED and body == Global.player:
    _go_to_walk_phase()

func _create_temple_gems():
  var i = 0
  for color_grp in Constants.COLOR_GROUPS:
    if Global.gem_hud.is_gem_collected(color_grp):
      var temple_gem = _create_temple_gem(
        color_grp,
        WAIT_DELAY * (i+1),
        Global.player.global_position,
        GemsSlotsNodes[i].global_position,
        GEMS_EASE_TYPE[i])
      temple_gems.append(temple_gem)
      num_active_gems += 1
      i += 1
  return i > 0 #returns if we at least created a gem

func _create_temple_gem(color_group: String, delay: float, _position: Vector2, _destination: Vector2, ease_type):
  var temple_gem = TempleGemScene.instance()
  temple_gem.color_group = color_group
  GemSlotsContainerNode.add_child(temple_gem)
  temple_gem.set_owner(GemSlotsContainerNode)
  temple_gem.set_deferred("global_position", _position)
  var wait_time = delay
  temple_gem.call_deferred("move_to_position", _destination, wait_time, ease_type)
  var __ = temple_gem.connect("move_completed", self, "_on_gem_collected", [], CONNECT_ONESHOT)
  return temple_gem

func _process(_delta):
  if current_state == States.WALK_PHASE:
    Global.player.SetMaxSpeed()
  elif current_state == States.ROTATION_PHASE:
    _process_rotate_gems(_delta)
  elif current_state == States.BLOOM_PHASE:
    BloomSpriteNode.scale = Vector2(bloom_sprite_scale, bloom_sprite_scale)

func _process_rotate_gems(_delta: float):
  var amount = gems_angular_velocity*_delta
  GemSlotsContainerNode.rotate(amount)
  for node in GemSlotsContainerNode.get_children():
    node.rotate(-amount)
  var intensity = 0.5 + (gems_angular_velocity / MAX_GEM_TEMPLE_ANGULAR_VELOCITY)
  for node in temple_gems:
    node.set_light_intensity(intensity)

func _on_gem_collected(_temple_gem):
  if current_state == States.COLLECT_PHASE:
    num_active_gems -= 1
    Event.emit_gem_put_in_temple()
    if num_active_gems <= 0:
      _go_to_rotation_phase()

func _start_rotation_timer():
  RotationTimerNode.wait_time = ROTATION_DURATION
  RotationTimerNode.start()

func _on_StartGemsArea_body_entered(body:Node):
  if current_state == States.WALK_PHASE and body == Global.player:
    _go_to_collect_phase()

func _on_RotationTimer_timeout():
  if current_state == States.ROTATION_PHASE:
    _go_to_bloom_phase()

#state transitions:
func _go_to_not_triggered_phase():
  current_state = States.NOT_TRIGGERED
  for el in temple_gems:
    el.queue_free()
  temple_gems = []
  gems_angular_velocity = 0
  num_active_gems = 0
  bloom_sprite_scale = 1.0
  BloomSpriteNode.visible = false

func _go_to_walk_phase():
  current_state = States.WALK_PHASE
  Global.player.velocity.x = 0
  Event.emit_gem_temple_triggered()
  Global.cutscene._show_some_node(Global.player.global_position, 10.0, 3.2)

func _go_to_rotation_phase():
  current_state = States.ROTATION_PHASE
  _rotate_gems_tween()
  _start_rotation_timer()
  Event.emit_gem_engine_started()

func _go_to_collect_phase():
  current_state = States.COLLECT_PHASE
  Global.player.velocity.x = 0
  if !_create_temple_gems():
    _on_gem_collected(null) #case of no gems to collect

func _go_to_bloom_phase():
  current_state = States.BLOOM_PHASE
  BloomSpriteNode.visible = true
  _bloom_sprite_resize_tween()

func _go_to_end_phase():
  Event.emit_level_cleared()

#tween animations
func _rotate_gems_tween():
  if tweener:
    tweener.kill()
  tweener = create_tween()
  var __ = tweener.tween_property(
    self,
    "gems_angular_velocity",
    MAX_GEM_TEMPLE_ANGULAR_VELOCITY,
    DURATION_TO_FULL_GEM_ROTATION_SPEED
  ).from(gems_angular_velocity
  ).set_trans(Tween.TRANS_LINEAR
  ).set_ease(Tween.EASE_OUT)

func _bloom_sprite_resize_tween():
  if tweener:
    tweener.kill()
  tweener = create_tween()
  var __ = tweener.tween_property(
    self,
    "bloom_sprite_scale",
    BLOOM_SPRITE_MAX_SCALE,
    BLOOM_SPRITE_SCALE_DURATION
  ).from(bloom_sprite_scale
  ).set_trans(Tween.TRANS_LINEAR
  ).set_ease(Tween.EASE_OUT)
  
  __ = tweener.connect("finished", self, "_on_bloom_sprite_resize_end", [], CONNECT_ONESHOT)

func _on_bloom_sprite_resize_end():
  _go_to_end_phase()
