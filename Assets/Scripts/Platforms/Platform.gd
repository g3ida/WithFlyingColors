tool
extends KinematicBody2D

#imports
const NinePatchTextureUtils = preload("res://Assets/Scripts/Utils/NinePatchTextureUtils.gd")

#exported vars
export var texture: Texture
export var group: String

#vars
var animation_timer = 10
var contactPosition = Vector2(0, 0)
onready var ninePatchUtils = NinePatchTextureUtils.new()


func _ready() -> void:
  self.ninePatchUtils.set_texture($NinePatchRect, texture)
  self.ninePatchUtils.scale_texture($NinePatchRect, self.scale)
  $Area2D.add_to_group(group)
  
func _enter_tree():
  connect_signals()

func _exit_tree():
  disconnect_signals()

func _on_Player_landed(area, position):
  if area == $Area2D:
    animation_timer = 0
    contactPosition = position  
  
func _process(delta):
  if Engine.editor_hint:
    return
  animation_timer += delta
  var shaderMaterial = $NinePatchRect.material as ShaderMaterial
  if (shaderMaterial != null):
    var resolution = get_viewport().size
    var cam = Global.camera
    if (cam != null):
      var camPos = cam.get_camera_screen_center()
      var cuurent_pos = Vector2(
        self.contactPosition.x + (resolution.x / 2) - camPos.x,
        self.contactPosition.y + (resolution.y / 2) - camPos.y)
      var pos = Vector2(cuurent_pos.x / resolution.x, cuurent_pos.y / resolution.y)
      var position_in_shader_coords = Vector2(pos.x, 1-pos.y)
      shaderMaterial.set_shader_param("u_contact_pos", position_in_shader_coords)
      shaderMaterial.set_shader_param("u_timer", animation_timer)
      shaderMaterial.set_shader_param("u_aspect_ratio", resolution.y / resolution.x)

func connect_signals():
  Event.connect("player_landed", self, "_on_Player_landed")
  
func disconnect_signals():
  Event.disconnect("player_landed", self, "_on_Player_landed")
