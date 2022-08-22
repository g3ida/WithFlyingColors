extends Tabs


func _ready():
  pass


func _on_TabContainer_tab_changed(tab):
  if get_index() == tab:
    get_child(0).on_gain_focus()
