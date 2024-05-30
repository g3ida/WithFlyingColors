extends Area2D

var follow_child = null

var triggered = false

func _ready():
  var children = get_children()
  for ch in children:
    if ch is Position2D:
      follow_child = ch

func _on_Area2D_body_entered(body):
  if !triggered and body == Global.player and follow_child != null:
    triggered = true
    Global.cutscene.ShowSomeNode(follow_child, 3.0, 3.2)
