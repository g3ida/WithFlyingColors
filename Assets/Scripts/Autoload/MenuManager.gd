extends Node2D

const SETTINGS_MENU_SCENE = "res://Assets/Screens/SettingsMenu.tscn"
const STATS_MENU_SCENE = "res://Assets/Screens/StatsMenu.tscn"
const MAIN_MENU_SCENE = "res://Assets/Screens/MainMenu.tscn"
const LOAD_MENU_SCENE = "res://Assets/Screens/LoadGameMenu.tscn"
const TUTORIAL_MENU_SCENE = "res://Levels/Level1.tscn"
const SCENE_ORCHESTER_SCENE = "res://Assets/Scenes/SceneOrchester.tscn"

enum Menus {
  SETTINGS_MENU,
  PLAY_MENU,
  STATS_MENU,
  MAIN_MENU,
  GAME,
  QUIT
}

var current_menu = Menus.MAIN_MENU
var previous_menu = Menus.PLAY_MENU

func get_menu_scene_path(menu):
  if menu == Menus.SETTINGS_MENU:
    return SETTINGS_MENU_SCENE
  if menu == Menus.STATS_MENU:
    return STATS_MENU_SCENE
  if menu == Menus.PLAY_MENU:
    return LOAD_MENU_SCENE
  if menu == Menus.MAIN_MENU:
    return MAIN_MENU_SCENE
  if menu == Menus.GAME:
    return SCENE_ORCHESTER_SCENE
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
