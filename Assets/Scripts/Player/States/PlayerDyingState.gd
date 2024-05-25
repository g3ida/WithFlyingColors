class_name PlayerDyingState
extends PlayerBaseState

const ExplosionScene = preload("res://Assets/Scenes/Explosion/Explosion.tscn")

var death_animation_type = DeathAnimationType.DYING_EXPLOSION

var light_mask: int

func _init(dependencies: PlayerDependencies).(dependencies):
  light_mask = light_occluder.light_mask
  self.base_state = PlayerStatesEnum.DYING

func enter():
  player.hide_color_areas()
  player.set_collision_shapes_disabled_flag_deferred(true)
  if (death_animation_type == DeathAnimationType.DYING_EXPLOSION_REAL):
    call_deferred("_create_explosion")
    Event.emit_signal("player_explode")
    light_occluder.light_mask = 0
    animated_sprite.play("die")
  elif (death_animation_type == DeathAnimationType.DYING_EXPLOSION):
    Event.emit_signal("player_explode")
    light_occluder.light_mask = 0
    animated_sprite.play("die")
    yield(animated_sprite, "animation_finished")
    light_occluder.light_mask = light_mask
    Event.emit_signal("player_died")
  else: # this is the falling case
    Event.emit_signal("player_fall")
    player.fallTimerNode.start()
    yield(player.fallTimerNode, "timeout")
    Event.emit_signal("player_died")

func physics_update(delta: float) -> BaseState:
  return .physics_update(delta)

func _on_player_diying(_area, _position, _entity_type) -> BaseState:
    return null

func exit():
  player.show_color_areas()
  player.set_collision_shapes_disabled_flag_deferred(false)

func _physics_update(_delta: float) -> BaseState:
  return null

func _create_explosion():
  var explosion = ExplosionScene.instance()
  explosion.player = Global.player
  explosion.playerTexture = Global.get_player_sprite()
  explosion.connect("ObjectDetonated", self, "_on_object_detonated", [], CONNECT_ONESHOT)
  explosion.connect("ready", self, "_explosion_is_ready", [explosion], CONNECT_ONESHOT)
  Global.player.add_child(explosion)
  explosion.set_owner(Global.player)

func _explosion_is_ready(explosion):
  explosion.Setup()
  explosion.FireExplosion()

func _on_object_detonated(explosion):
  explosion.queue_free()
  Event.emit_signal("player_died")
