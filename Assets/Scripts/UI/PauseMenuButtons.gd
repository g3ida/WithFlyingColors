extends Button 

onready var animation_player = $AnimationPlayer

enum {HIDDEN, HIDING, SHOWN, SHOWING}
var current_state = HIDDEN

func hide():
  if current_state == SHOWN:
    current_state = HIDING
    animation_player.play("Hide")
    var __ = animation_player.connect("animation_finished", self, "_on_show_animation_done", [], CONNECT_ONESHOT)
  elif current_state == SHOWING:
    animation_player.disconnect("animation_finished", self, "_on_hide_animation_done")
    current_state = SHOWN
    hide()

func show():
  if current_state == HIDDEN:
    current_state = SHOWING
    self.visible = true
    animation_player.play_backwards("Hide")
    var __ = animation_player.connect("animation_finished", self, "_on_show_animation_done", [], CONNECT_ONESHOT)
  elif current_state == HIDING:
    animation_player.disconnect("animation_finished", self, "_on_hide_animation_done")
    current_state = HIDDEN
    show()

func _ready():
  self.visible = false
  animation_player.play("Hidden")
    
func _on_hide_animation_done():
  self.visible = false
  animation_player.play("Hidden")
  current_state = HIDDEN

func _on_show_animation_done():
  self.visible = true
  animation_player.play("Shown")
  current_state = SHOWN
