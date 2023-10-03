extends GameMenu

const X_POS = 1000
const Y_POS = 200
const Y_STEP = 300

onready var LevelsContainer = $LevelsContainer
onready var sceneCards = []

const SceneCardScene = preload("res://Assets/Scenes/LevelSelectMenu/SceneCard.tscn")

func _ready():
  _populate_with_cards()

func _populate_with_cards():
  for level in Levels.LEVELS:
    var sceneCard = _add_scene_card(level)
    sceneCards.append(sceneCard)
  sceneCards[-1].set_focus()

func _add_scene_card(level):
  var sceneNode = SceneCardScene.instance()
  LevelsContainer.add_child(sceneNode)
  var id = level["id"]
  sceneNode.set_owner(LevelsContainer)
  sceneNode.set_deferred("levelScene", level["scene"]) 
  sceneNode.set_deferred("levelName", "%d.%s" % [id, level["name"]])
  return sceneNode

func _on_BackButton_pressed():
  if not is_in_transition_state():
    Event.emit_menu_button_pressed(MenuButtons.BACK)
