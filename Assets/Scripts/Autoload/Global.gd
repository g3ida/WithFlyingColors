extends Node2D

const EPSILON: float = 0.0001
const WORLD_TO_SCREEN = 100

var camera: Camera2D = null
var player: KinematicBody2D = null

enum EntityType {PLATFORM, FALLZONE, LAZER}
enum {SETTINGS_MENU, PLAY_MENU, STATS_MENU, MAIN_MENU}

var currentMenu = MAIN_MENU
var PreviousMenu = PLAY_MENU

func _ready():
  var __ = Event.connect("Stats_button_pressed", self, "_on_menu_change_to_stats")
  __ = Event.connect("Play_button_pressed", self, "_on_menu_change_to_play")
  __ = Event.connect("Settings_button_pressed", self, "_on_menu_change_to_settings")
  __ = Event.connect("Go_to_main_menu_pressed", self, "_on_menu_change_to_main")

func _on_menu_change_to_stats():
  _on_menu_change(STATS_MENU)

func _on_menu_change_to_play():
  _on_menu_change(PLAY_MENU)

func _on_menu_change_to_settings():
  _on_menu_change(SETTINGS_MENU)

func _on_menu_change_to_main():
  _on_menu_change(MAIN_MENU)

func _on_menu_change(next_menu):
  if (currentMenu != next_menu):
    PreviousMenu = currentMenu
    currentMenu = next_menu
