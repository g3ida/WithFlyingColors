extends Node2D

const SETTINGS_MENU_SCENE = "res://Assets/Screens/SettingsMenu.tscn"
const STATS_MENU_SCENE = "res://Assets/Screens/StatsMenu.tscn"
const MAIN_MENU_SCENE = "res://Assets/Screens/MainMenu.tscn"
const PLAY_MENU_SCENE = "res://Assets/Screens/PlayMenu.tscn"

enum Menus {SETTINGS_MENU, PLAY_MENU, STATS_MENU, MAIN_MENU, QUIT}

var current_menu = Menus.MAIN_MENU
var previous_menu = Menus.PLAY_MENU

func get_menu_scene_path(menu):
  if menu == Menus.SETTINGS_MENU:
    return SETTINGS_MENU_SCENE
  if menu == Menus.STATS_MENU:
    return STATS_MENU_SCENE
  if menu == Menus.STATS_MENU:
    return PLAY_MENU_SCENE
  if menu == Menus.MAIN_MENU:
    return MAIN_MENU_SCENE
  return null

func go_to_menu(next_menu):
  if (current_menu != next_menu):
    previous_menu = current_menu
    current_menu = next_menu 
    var __ = get_tree().change_scene(get_menu_scene_path(next_menu))

func _ready():
  set_process(false)
