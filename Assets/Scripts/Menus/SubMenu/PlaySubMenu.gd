tool
extends Control

const SubMenuScene = preload("res://Assets/Scenes/MainMenu/SubMenu.tscn")

const SHOULD_HIDE_DISABLED_BUTTONS = true #set this to false to display disabled buttons

onready var SubMenuNode = null

enum ButtonConditions {
  NONE,
  NEED_ACTIVE_SLOT, # an active slot contains a game in progress not a won game
  NEED_NEW_SLOT, # a new slot with progress 0%
}

var buttons_def = [
  {
    "id": 0,
    "text": "Continue",
    "button": MenuButtons.CONTINUE_GAME,
    "conditions": ButtonConditions.NEED_ACTIVE_SLOT
  },
  {
    "id": 1,
    "text": "New Game",
    "button": MenuButtons.NEW_GAME,
    "conditions": ButtonConditions.NEED_NEW_SLOT
  },
  {
    "id": 3,
    "text": "Select Level",
    "button": MenuButtons.SELECT_LEVEL,
    "conditions": ButtonConditions.NEED_ACTIVE_SLOT
  },
  {
    "id": 4,
    "text": "Current Slot: %s" % (SaveGame.current_slot_index + 1),
    "button": MenuButtons.SELECT_SLOT,
    "conditions": ButtonConditions.NONE
  },
]

func _ready():
  var color_index = ColorUtils.get_group_color_index("blue")
  var blue = ColorUtils.get_skin_basic_color(SkinLoader.DEFAULT_SKIN, color_index)
  SubMenuNode = SubMenuScene.instance()
  SubMenuNode.color = ColorUtils.darken_rgb(blue, 0.0)
  SubMenuNode.top_color = ColorUtils.darken_rgb(blue, 0.115)
  
  SubMenuNode.buttons = []
  SubMenuNode.buttons_events = []
  SubMenuNode.buttons_ids = []
  SubMenuNode.buttons_disabled = []
  
  for btn in buttons_def:
    if SHOULD_HIDE_DISABLED_BUTTONS and _should_disable_button(btn): continue
    SubMenuNode.buttons.append(btn["text"])
    SubMenuNode.buttons_events.append(btn["button"])
    SubMenuNode.buttons_ids.append(btn["id"])
    SubMenuNode.buttons_disabled.append(true if _should_disable_button(btn) else false)
  add_child(SubMenuNode)
  SubMenuNode.set_owner(self)
  rect_min_size = SubMenuNode.rect_min_size
  rect_size = SubMenuNode.rect_size

func _should_disable_button(btn):
  if btn["conditions"] == ButtonConditions.NEED_ACTIVE_SLOT:
    return not SaveGame.does_slot_have_progress(SaveGame.current_slot_index)
  if btn["conditions"] == ButtonConditions.NEED_NEW_SLOT:
    return SaveGame.does_slot_have_progress(SaveGame.current_slot_index)
