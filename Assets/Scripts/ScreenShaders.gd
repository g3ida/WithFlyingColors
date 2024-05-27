extends CanvasLayer

onready var darker_shader := $DarkerShader/ColorRect
onready var simple_blur := $SimpleBlur/ColorRect
onready var darker_shader_animation_player := $DarkerShader/ColorRect/AnimationPlayer
onready var simple_blur_animation_player := $SimpleBlur/ColorRect/AnimationPlayer

enum {DISABLED, TRANSITION_IN, ENABLED, TRANSITION_OUT}
var current_state = DISABLED

func _ready():
  darker_shader.visible = false
  simple_blur.visible = false
  darker_shader_animation_player.play("RESET")
  simple_blur_animation_player.play("RESET")
  
func activate_pause_shader():
  if (current_state == DISABLED):
    darker_shader.visible = true
    simple_blur.visible = true
    darker_shader_animation_player.play("Blackout")
    simple_blur_animation_player.play("Blur")
    var __ = darker_shader_animation_player.connect("animation_finished", self, "_on_blackout_animation_finished", [], CONNECT_ONESHOT)
    current_state = TRANSITION_IN
  elif current_state == TRANSITION_OUT:
    darker_shader_animation_player.disconnect("animation_finished", self, "_on_blackout_animation_reversed_finished")
    current_state = DISABLED
    activate_pause_shader()
  
func disable_pause_shader():
  if (current_state == ENABLED):
    darker_shader_animation_player.play_backwards("Blackout")
    simple_blur_animation_player.play_backwards("Blur")
    var __ = darker_shader_animation_player.connect("animation_finished", self, "_on_blackout_animation_reversed_finished", [], CONNECT_ONESHOT)
    current_state = TRANSITION_OUT
  elif current_state == TRANSITION_IN:
    darker_shader_animation_player.disconnect("animation_finished", self, "_on_blackout_animation_finished")
    current_state = ENABLED
    disable_pause_shader()
    
  
func _on_blackout_animation_reversed_finished(_animation_name):
  darker_shader_animation_player.play("RESET")
  simple_blur_animation_player.play("RESET")
  darker_shader.visible = false
  simple_blur.visible = false
  current_state = DISABLED

func _on_blackout_animation_finished(_animation_name):
  darker_shader.visible = true
  simple_blur.visible = true
  current_state = ENABLED
