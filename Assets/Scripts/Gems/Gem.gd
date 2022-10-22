class_name Gem
extends Area2D

export var group_name: String

onready var lightNode = $Light2D
onready var shineNode = $ShineSfx

var states_store
var current_state: GemBaseState

var save_data = {
  "state": null
}

func _ready():
  self.add_to_group(group_name)
  var color_index = ColorUtils.get_group_color_index(group_name)
  var color = ColorUtils.get_basic_color(color_index)
  lightNode.color = color
  $AnimatedSprite.modulate = color
  
  states_store = GemStatesStore.new(
    self,
    $Light2D,
    $AnimatedSprite,
    $AnimatedSprite/AnimationPlayer,
    $CollisionShape2D,
    shineNode)
    
  current_state = states_store.not_collected
  save_data["state"] = states_store.get_state_enum(current_state)
  current_state.enter()

func switch_state(new_state):
  if (new_state != null):
    current_state.exit()
    current_state = new_state
    current_state.enter()

func _physics_process(delta):
  var state = current_state.physics_update(delta)
  switch_state(state)

func _on_Gem_area_entered(area: Area2D): 
  var player_state = Global.player.player_state.base_state
  if player_state == PlayerStatesEnum.DYING or current_state != states_store.not_collected:
    return
  var state = current_state.on_collision_with_body(area)
  switch_state(state)
    
func _on_AnimationPlayer_animation_finished(anim_name):
  var state = current_state.on_animation_finished(anim_name)
  switch_state(state)

func connect_signals():
  var __ = Event.connect("checkpoint_reached", self, "_on_checkpoint_hit")
  __ = Event.connect("checkpoint_loaded", self, "reset")
    
func disconnect_signals():
  Event.disconnect("checkpoint_reached", self, "_on_checkpoint_hit")
  Event.disconnect("checkpoint_loaded", self, "reset")
        
func _enter_tree():
  connect_signals()
  
func _exit_tree():
  disconnect_signals()

func _on_checkpoint_hit(_checkpoint):
  var saved_state = current_state if current_state != states_store.collecting else states_store.collected
  save_data["state"] = states_store.get_state_enum(saved_state)

func save():
  return save_data

func reset():
  switch_state(states_store.get_state(save_data["state"]))

func is_in_group(grp) -> bool:
  #if the player is dying we don't want to collect it
  var player_state = Global.player.player_state.base_state
  if player_state == PlayerStatesEnum.DYING:
    return false
  #if the gem is already collecting we don't wan't the player to die
  if current_state == states_store.collecting:
    if grp in ["blue", "yellow", "purple", "pink"]:
      return true
  #return super method
  return .is_in_group(grp)
