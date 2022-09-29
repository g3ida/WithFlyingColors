class_name PlayerStandingState
extends PlayerBaseState

const RAYCAST_LENGTH = 10.0
const RAYCAST_Y_OFFSET = -3.0 #https://godotengine.org/qa/63336/raycast2d-doesnt-collide-with-tilemap
const SLIPPERING_LIMIT = 0.42 # higher is less slippering

func _init(dependencies: PlayerDependencies).(dependencies):
  self.base_state = PlayerStatesEnum.STANDING

func enter():
  self.animated_sprite.play("idle")
  animated_sprite.playing = false
  Event.emit_signal("player_land")
  player.can_dash = true

func exit():
  pass

func physics_update(delta: float) -> BaseState:
  return .physics_update(delta)

func _physics_update(_delta: float) -> BaseState:
  if _jump_pressed() and player.is_on_floor():
    return _on_jump()
  if not player.is_on_floor():
    var falling_state = self.states_store.get_state(PlayerStatesEnum.FALLING)
    falling_state.was_on_floor = true
    return falling_state
  else:
    if (abs(player.velocity.x) < player.speed_unit\
      and player.player_rotation_state.base_state == PlayerStatesEnum.IDLE):
      return raycast_floor()  
  return null

func raycast_floor():
  var space_state = player.get_world_2d().direct_space_state
  var player_half_size = player.get_collision_shape_size() * 0.5 * player.scale
  
  var combination = 0
  var i = 1
  var from_offset_x = [
    -player_half_size.x,
    -player_half_size.x*SLIPPERING_LIMIT,
    player_half_size.x*SLIPPERING_LIMIT,
    player_half_size.x
  ]

  for offset in from_offset_x:
    var from := player.global_position + Vector2(offset, player_half_size.y+RAYCAST_Y_OFFSET)
    var to := from + Vector2(0.0, RAYCAST_LENGTH)
    var result = space_state.intersect_ray(from, to, [player])
    if (!result.empty()):
      combination += i
    i*=2
  if combination == 1 or combination == 8: #flags values
    var slippering_state = self.states_store.get_state(PlayerStatesEnum.SLIPPERING)
    slippering_state.direction = 1 if combination == 1 else -1
    return slippering_state
  return null
    
