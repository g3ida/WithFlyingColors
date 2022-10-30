extends Node2D

signal bricks_cleared()
signal level_cleared(level)

onready var levels = get_children()
onready var is_level_cleared = []
export var should_instance_bricks = true

var color_groups = Constants.COLOR_GROUPS
var least_uncleared_level = 0

func _ready():
  _init_is_level_cleared()
      
func _init_is_level_cleared():
  is_level_cleared = []
  for _i in range(levels.size()):
    is_level_cleared.append(!should_instance_bricks) 
        
func _get_least_uncleared_level():
  for i in range(is_level_cleared.size()):
    if !is_level_cleared[i]:
      return i
  return -1

func _on_level_bricks_cleared(id):
  is_level_cleared[id] = true
  var new_uncleated_level = _get_least_uncleared_level()
  if new_uncleated_level == -1:
    call_deferred("emit_signal","bricks_cleared")
  elif new_uncleated_level != least_uncleared_level:
    call_deferred("emit_signal","level_cleared", new_uncleated_level)
    
func _emit_level_cleared(new_uncleated_level):
  emit_signal("level_cleared", new_uncleated_level)
  
func _emit_bricks_cleared():
  emit_signal("bricks_cleared")
