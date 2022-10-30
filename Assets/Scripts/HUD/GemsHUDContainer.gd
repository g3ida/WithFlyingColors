extends Panel

onready var BlueGemNode = $BlueGem
onready var PinkGemNode = $PinkGem
onready var YellowGemNode = $YellowGem
onready var PurpleGemNode = $PurpleGem

onready var GemsNodes: Dictionary = {
  "blue": BlueGemNode,
  "pink": PinkGemNode,
  "yellow": YellowGemNode,
  "purple": PurpleGemNode,
}

func _ready():
  Global.gem_hud = self

func is_gem_collected(color_group: String):
  if GemsNodes.has(color_group):
    var gem_node = GemsNodes[color_group]
    if gem_node.current_state == gem_node.COLLECTED:
      return true
  return false
