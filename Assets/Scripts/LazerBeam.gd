tool
extends Node2D

onready var beamNode = $Line2D
onready var beamBgNode = $Line2Dbackground
onready var muzzleNode = $Muzzle
onready var particlesNode = $Particles
onready var baseNode = $Base

export var color_group: String


func _ready():
  var color_index = ColorUtils.get_group_color_index(color_group)
  var color = ColorUtils.get_basic_color(color_index)
  beamNode.default_color = color
  beamBgNode.default_color = color
  beamBgNode.default_color.a = 0.63
  particlesNode.color = beamNode.default_color
  baseNode.modulate = color

func cast_beam():
  var space_state = get_world_2d().direct_space_state
  var result = space_state.intersect_ray(
    muzzleNode.global_position,
    muzzleNode.global_position + transform.x * 1000,
    [self],
    2147483647, #collision mask
    true, #collide with bodies
    true) #collide with areas
  var pos = transform.xform_inv(result.position)
  beamNode.set_point_position(1, pos)
  beamBgNode.set_point_position(1, pos)
  particlesNode.position = pos
  return result

func _physics_process(_delta):
  if Engine.editor_hint:
    return
  var cast_result = cast_beam()
  var collider = cast_result["collider"]
  if ("Face" in collider.name and collider is Area2D):
    var groups = collider.get_groups()
    if (groups and groups.size() == 1):
      if (groups.front() == color_group):
        pass #play some sfx maybe ?
      else:
        Event.emit_signal("player_diying", null, global_position, Constants.EntityType.LAZER)
