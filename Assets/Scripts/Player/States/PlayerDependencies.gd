class_name PlayerDependencies

var player
var body: KinematicBody2D
var light_occluder: LightOccluder2D
var animated_sprite: AnimatedSprite
var collision_shape: CollisionShape2D
var player_rotation_action: PlayerRotationAction
var states_store

func _init(the_player):
  self.player = the_player
  self.body = the_player as KinematicBody2D
  self.light_occluder = the_player.get_node("AnimatedSprite/LightOccluder2D")
  self.animated_sprite = the_player.get_node("AnimatedSprite")
  self.collision_shape = the_player.get_node("CollisionShape2D")
  self.player_rotation_action = the_player.playerRotationAction
