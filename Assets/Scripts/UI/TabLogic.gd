extends Tabs


func _ready():
  pass


func _on_TabContainer_tab_changed(tab):
  if get_index() == tab:
    Event.emit_signal("tab_changed")
    get_child(0).on_gain_focus()
