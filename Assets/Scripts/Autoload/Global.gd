extends Node2D

var camera: Camera2D = null
var HUD_offset: Vector2

enum {SETTINGS_MENU, PLAY_MENU, STATS_MENU, MAIN_MENU}

var currentMenu = MAIN_MENU
var PreviousMenu = PLAY_MENU

func _ready():
  Event.connect("Stats_button_pressed", self, "_on_menu_change_to_stats")
  Event.connect("Play_button_pressed", self, "_on_menu_change_to_play")
  Event.connect("Settings_button_pressed", self, "_on_menu_change_to_settings")
  Event.connect("Go_to_main_menu_pressed", self, "_on_menu_change_to_main")

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
