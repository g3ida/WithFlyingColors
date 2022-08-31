extends Node

signal player_landed(area, position)
signal player_diying(area, position, entity_type)
signal player_died()
signal player_jumped()
signal player_rotate(dir)
signal player_land()
signal player_explode()
signal player_fall()
signal player_dash(direction)

signal gem_collected(color, position, frames)
signal slide_animation_ended(animation_name)
signal checkpoint_reached(checkpoint_object)
signal checkpoint_loaded()

#UI Signals
signal Play_button_pressed()
signal Stats_button_pressed()
signal Settings_button_pressed()
signal Quit_button_pressed()
signal Go_to_main_menu_pressed()
signal menu_box_rotated()
signal pause_menu_enter()
signal pause_menu_exit()

#Settings signals
signal Fullscreen_toggled(value)
signal Vsync_toggled(value)
signal Screen_size_changed(value)
signal on_action_bound(action, key)
signal tab_changed()
signal focus_changed()
signal keyboard_action_biding()