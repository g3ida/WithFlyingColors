extends TileMap

#exported vars
export var splash_darkness = 0.78

#vars
var animation_timer = 10
var contactPosition = Vector2(0, 0)
  
func _enter_tree():
  connect_signals()

func _exit_tree():
  disconnect_signals()

func _on_Player_landed(_area, position):
  if _area == $Area2D:
    animation_timer = 0
    contactPosition = position
  
func _process(delta):
  if Engine.editor_hint:
    return
  animation_timer += delta
  var shaderMaterial = self.material as ShaderMaterial
  if (shaderMaterial != null):
    var resolution = get_viewport().get_size_override()
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
      shaderMaterial.set_shader_param("darkness", splash_darkness)
      
func connect_signals():
  var __ = Event.connect("player_landed", self, "_on_Player_landed")
  
func disconnect_signals():
  Event.disconnect("player_landed", self, "_on_Player_landed")
