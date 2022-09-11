extends Node2D

export(String) var color_group
export(Texture) var texture
export(PackedScene) var on_hit_script

signal on_player_hit(emitter, on_hit_script)

onready var AreaNode = $Area2D
onready var BackgroundNode = $Background
onready var SpriteNode = $Sprite

const SPEED = 3.0 * Global.WORLD_TO_SCREEN

func _ready():
  SpriteNode.texture = texture
  BackgroundNode.modulate = ColorUtils.get_color(color_group)
  AreaNode.add_to_group(color_group)
  
func _process(delta):
  position.y += SPEED * delta
  #check collision with deadzone

func _on_Area2D_body_entered(body):
  if body == Global.player:
    if Global.player.player_state.base_state !=  PlayerStatesEnum.DYING:
      emit_signal("on_player_hit", self, on_hit_script)
      queue_free()

func _on_Area2D_area_entered(area):
  if area.is_in_group("death_zone"):
    queue_free()
