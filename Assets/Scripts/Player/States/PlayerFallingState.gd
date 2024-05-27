class_name PlayerFallingState
extends PlayerBaseState

const CountdownTimer = preload("res://Assets/Scripts/Utils/CountdownTimer.cs")
var permissiveness_timer: CountdownTimer = CountdownTimer.new()
var was_on_floor = false
const PERMISSIVENESS = 0.04

func _init(dependencies: PlayerDependencies).(dependencies):
  permissiveness_timer.Set(PERMISSIVENESS, false)
  self.base_state = PlayerStatesEnum.FALLING

func enter():
  animated_sprite.play("idle")
  animated_sprite.playing = false
  if (was_on_floor):
    permissiveness_timer.Reset()
  jump_particles.emitting = true
  
func exit():
  permissiveness_timer.Stop()
  jump_particles.emitting = false

func physics_update(delta: float) -> BaseState:
  return .physics_update(delta)

func _physics_update(_delta: float) -> BaseState:
  if (player.is_on_floor()):
    return self.states_store.get_state(PlayerStatesEnum.STANDING)
  if _jump_pressed() and permissiveness_timer.IsRunning():
    permissiveness_timer.Stop()
    return _on_jump()
  permissiveness_timer.Step(_delta)
  return null

func on_animation_finished(_anim_name) -> BaseState:
  return null
