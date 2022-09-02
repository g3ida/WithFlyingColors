extends Node2D

onready var animationNode = $AnimationPlayer
onready var labelAnimationNode = $Label/LabelAnimation

var animation_count = 0

func _ready():
  animationNode.play("Scale")
  labelAnimationNode.play("Fade")


func _on_LabelAnimation_animation_finished(_anim_name):
  on_anim_end()


func _on_AnimationPlayer_animation_finished(_anim_name):
  on_anim_end()
  
func on_anim_end():
  animation_count+=1
  if animation_count == 2:
    queue_free()
