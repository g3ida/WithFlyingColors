extends Node2D

const BLOCKS_PER_SIDE = 6
const BLOCKS_IMPULSE = 400
const BLOCKS_GRAVITY_SCALE = 20
const DEBRIS_MAX_TIME = 1.5

const ExplosionElementScene = preload("res://Assets/Scenes/Explosion/ExplosionElement.tscn")

signal object_detonated(this)

var is_randomize_seed = false
var object = {}
export var player_texture: Texture
onready var player: KinematicBody2D = get_parent()

func fire_explosion():
  if object.can_detonate:
    object.detonate = true
    call_deferred("_explode")

func _instance_explosion_element(n: int):
  var explosion_element = ExplosionElementScene.instance()
  explosion_element.name = self.name + "_block_" + str(n)
  var shape = RectangleShape2D.new()
  shape.extents = object.collision_extents
  explosion_element.mode = RigidBody2D.MODE_STATIC
  explosion_element.setup_sprite(player_texture, object.vframes, object.hframes, n)
  explosion_element.set_collider_shape(shape)
  explosion_element.get_collider().disabled = true
  explosion_element.visible = false
  return explosion_element

func setup():
  object = {
    blocks_container = Node2D.new(),
    can_detonate = true,
    collision_extents = Vector2(),
    debris_timer = Timer.new(),
    detonate = false,
    has_detonated = false,
    height = 0,
    hframes = 1,
    offset = Vector2(),
    vframes = 1,
    width = 0,
  }

  if is_randomize_seed: randomize()
  _set_debris_timer()
  object.vframes = BLOCKS_PER_SIDE
  object.hframes = BLOCKS_PER_SIDE
  object.width = player_texture.get_width()
  object.height = player_texture.get_height()
  object.offset = Vector2(object.width*.5, object.height*.5)  
  object.collision_extents = Vector2((object.width*.5)/object.hframes, (object.height*.5)/object.vframes)

  var idx = 0; var elems = []
  for x in range(object.hframes):
    for y in range(object.vframes):
      var explosion_element = _instance_explosion_element(idx)
      elems.append(explosion_element)
      explosion_element.position = Vector2(
        y * (object.width / object.hframes) - object.offset.x + object.collision_extents.x + position.x,
        x * (object.height / object.vframes) - object.offset.y + object.collision_extents.y + position.y)
      idx += 1
  call_deferred("add_children", object, elems)

func _set_debris_timer():
  object.debris_timer.connect("timeout", self ,"_on_debris_timer_timeout", [], CONNECT_ONESHOT) 
  object.debris_timer.set_one_shot(true)
  object.debris_timer.set_wait_time(DEBRIS_MAX_TIME)
  object.debris_timer.name = "debris_timer"
  add_child(object.debris_timer, true)

func _explode():
  for i in range(object.blocks_container.get_child_count()):
    var child = object.blocks_container.get_child(i)
    child.detonate(BLOCKS_IMPULSE)

func _physics_process(_delta):
  if object.can_detonate and object.detonate:
    detonate()

func add_children(child_object, elems):
  for i in range(elems.size()):
    child_object.blocks_container.add_child(elems[i], true)
  self.add_child(child_object.blocks_container)
  child_object.blocks_container.set_owner(self)

func detonate():
  object.can_detonate = false
  object.has_detonated = true
  object.detonate = false
  for i in range(object.blocks_container.get_child_count()):
    var child = object.blocks_container.get_child(i)
    child.gravity_scale = BLOCKS_GRAVITY_SCALE
    var child_scale = rand_range(0.5, 1.5)
    child.scale = Vector2(child_scale, child_scale)
    child.mass = child_scale
    child.set_collision_layer(0 if randf() < 0.5 else player.collision_layer)
    child.set_collision_mask(0 if randf() < 0.5 else player.collision_mask)
    child.z_index = 0 if randf() < 0.5 else -1
    child.mode = RigidBody2D.MODE_RIGID
    child.get_collider().disabled = false
    child.visible = true
  object.debris_timer.start()
        
func _on_debris_timer_timeout():
  for i in range(object.blocks_container.get_child_count()):
    var child = object.blocks_container.get_child(i)
    child.mode = RigidBody2D.MODE_STATIC
  emit_signal("object_detonated", self)
