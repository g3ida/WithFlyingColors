extends Node2D

const SETTINGS_MENU_SCENE = "res://Assets/Screens/SettingsMenu.tscn"
const STATS_MENU_SCENE = "res://Assets/Screens/StatsMenu.tscn"
const MAIN_MENU_SCENE = "res://Assets/Screens/MainMenu.tscn"
const SELECT_SLOT_SCENE = "res://Assets/Screens/SelectSlotMenu.tscn"
const START_LEVEL_MENU_SCENE = "res://Levels/Level1.tscn"
#const START_LEVEL_MENU_SCENE = "res://Levels/TutorialLevel.tscn"
const SCENE_ORCHESTER_SCENE = "res://Assets/Scenes/SceneOrchester.tscn"
const LEVEL_CLEAR_SCENE = "res://Assets/Screens/LevelClearedMenu.tscn"

enum Menus {
  SETTINGS_MENU,
  SELECT_SLOT,
  STATS_MENU,
  MAIN_MENU,
  LEVEL_CLEAR_MENU,
  GAME,
  QUIT,
  LOAD
}

var current_menu = Menus.MAIN_MENU
var previous_menu = Menus.GAME

func get_menu_scene_path(menu):
  if menu == Menus.SETTINGS_MENU:
    return SETTINGS_MENU_SCENE
  if menu == Menus.STATS_MENU:
    return STATS_MENU_SCENE
  if menu == Menus.MAIN_MENU:
    return MAIN_MENU_SCENE
  if menu == Menus.GAME:
    return SCENE_ORCHESTER_SCENE
  if menu == Menus.LEVEL_CLEAR_MENU:
    return LEVEL_CLEAR_SCENE
  if menu == Menus.SELECT_SLOT:
    return SELECT_SLOT_SCENE
  return null

func go_to_menu(next_menu):
  if (current_menu != next_menu):
    previous_menu = current_menu
    current_menu = next_menu 
    var scene_path = get_menu_scene_path(next_menu)
    if scene_path != null:
      var __ = get_tree().change_scene(scene_path)

func _ready():
  set_process(false)
