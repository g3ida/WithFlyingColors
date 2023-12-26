extends Sprite

var ghost_tween: SceneTreeTween

func _ready():
  ghost_tween = create_tween()
  var __ = ghost_tween.tween_property(self, "modulate:a", 0.0, 0.25
  ).set_trans(Tween.TRANS_QUART).set_ease(Tween.EASE_OUT)
  __ = ghost_tween.connect("finished", self, "_on_finish", [], CONNECT_ONESHOT)
  
func _on_finish():
  ghost_tween.kill()
  queue_free()
  
