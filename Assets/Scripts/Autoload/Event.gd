extends Node

signal player_landed(area, position)
signal player_diying(area, position)
signal player_died()
signal gem_collected(color, position)
signal slide_animation_ended(animation_name)
signal checkpoint_reached(checkpoint_object)
signal checkpoint_loaded()

#UI Signals
signal Play_button_pressed()
signal Stats_button_pressed()
signal Settings_button_pressed()
signal Quit_button_pressed()