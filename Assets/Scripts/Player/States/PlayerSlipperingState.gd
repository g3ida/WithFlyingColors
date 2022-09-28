class_name PlayerSlipperingState
extends PlayerBaseState

var direction := 1
var player_rotation: PlayerRotationAction

const RAYCAST_Y_OFFSET = -3.0
const RAYCAST_LENGTH = 5.0
const CORRECT_ROTATION_FALL_SPEED = 0.3
const CORRECT_ROTATION_JUMP_SPEED = 0.07
const PLAYER_SPEED_THRESHOLD_TO_STAND = -300.0
const SLIPPERING_ROTATION_DURATION = 3.0
const SLIPPERING_RECOVERY_INITIAL_DURATION = 0.8

var exit_rotation_speed = CORRECT_ROTATION_JUMP_SPEED
var skip_exit_rotation = false

func _init(dependencies: PlayerDependencies).(dependencies):
  self.base_state = PlayerStatesEnum.SLIPPERING
  player_rotation = dependencies.player_rotation_action

func enter():
  self.animated_sprite.play("idle")
  animated_sprite.playing = false
  skip_exit_rotation = false
  exit_rotation_speed = CORRECT_ROTATION_JUMP_SPEED
  self.player_rotation.execute(direction, Constants.PI2, SLIPPERING_ROTATION_DURATION, true, false, true)
  Event.emit_signal("player_slippering")
  player.can_dash = true

func exit():
  # the fact that I am splitting this into a slow then rapid action is for these reasons:
  # 1- to prevent collision if the player jumped (if rotation speed is high move_and_slide
  #    won't work because the player will touch the platform before jump is completed)
  # 2- to make falling less sudden (rotation should be slow for visual appeal and fast
  #    for gameplay so the combination is the best option )
  if not skip_exit_rotation:
    self.player_rotation.execute(-direction, Constants.PI2, SLIPPERING_RECOVERY_INITIAL_DURATION, true, false, false)
    yield(player.get_tree().create_timer(0.05), "timeout")
    self.player_rotation.execute(-direction, Constants.PI2, exit_rotation_speed, true, false, false)
  
func physics_update(delta: float) -> BaseState:
  return .physics_update(delta)

func _physics_update(_delta: float) -> BaseState:
  if _jump_pressed() and player.is_on_floor(): 
    exit_rotation_speed = CORRECT_ROTATION_JUMP_SPEED
    return _on_jump()

  if not player.is_on_floor():
    var falling_state = self.states_store.get_state(PlayerStatesEnum.FALLING)
    #addded to avoid complete rotation when fallign if the current angle is small enough or if the floor is
    #too close
    if abs(player.rotation - player_rotation.thetaZero) > Constants.PI8 and not check_if_ground_is_near():
      exit_rotation_speed = CORRECT_ROTATION_FALL_SPEED
      falling_state.was_on_floor = true
      direction = -direction
    return falling_state
  
  if (player.player_rotation_state.base_state != PlayerStatesEnum.IDLE):
    skip_exit_rotation = true
    return self.states_store.get_state(PlayerStatesEnum.STANDING)

  if self.player_rotation.canRotate or player_moved:
    return self.states_store.get_state(PlayerStatesEnum.STANDING)
  return _handle_ground_is_near()

func _get_falling_edge_position() -> Vector2:
  var corners = [
    player.FaceCollisionShapeTL_node,
    player.FaceCollisionShapeTR_node,
    player.FaceCollisionShapeBL_node,
    player.FaceCollisionShapeBR_node
  ]
  var pp = player.global_position
  var position = pp
  var size: Vector2 = player.get_collision_shape_size() * 0.5 * player.scale
  for cc in corners:
    var cp = cc.global_position
    if sign(pp.x - cp.x) == -direction and cp.y > position.y:
      position = cp
      size = cc.shape.extents
  return position + Vector2(-0.5*direction*size.x, 0.5*size.y) * player.scale

# the case of two grounds near each other with litte difference of hight
# we try to detect this case a give the player a litte push to fall on the
# near ground and avoid complete rotation
func check_if_ground_is_near():
  var space_state = player.get_world_2d().direct_space_state
  var from : Vector2 = _get_falling_edge_position() + Vector2.UP * RAYCAST_Y_OFFSET
  var to := from + Vector2(0.0, RAYCAST_LENGTH)
  var result = space_state.intersect_ray(from, to, [player])
  if (!result.empty()): #we hit something
    return true
  return false

func _handle_ground_is_near():
  if check_if_ground_is_near():
    self.player.velocity.x -= player.scale.x * direction * PLAYER_SPEED_THRESHOLD_TO_STAND
    return self.states_store.get_state(PlayerStatesEnum.STANDING)
  return null
