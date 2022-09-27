extends GameMenu

func _on_BackButton_pressed():
  Event.emit_menu_button_pressed(MenuButtons.BACK)
