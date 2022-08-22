tool
extends Node2D

const SlideAnimation = preload("res://Assets/Scripts/Utils/SlideAnimation.gd")

export var texture_collected: Texture
export var texture_empty: Texture
export var color: String

enum {EMPTY, COLLECTING, COLLECTED}
var current_state = EMPTY
var should_reset_state = true

var animation: AnimatedSprite
var collected_animation: SlideAnimation

# Called when the node enters the scene tree for the first time.
func _ready():
  $TextureRect.texture = texture_empty
func connect_signals():
  if not Engine.editor_hint:
    Event.connect("gem_collected", self, "_on_gem_collected")
    Event.connect("checkpoint_reached", self, "_on_checkpoint_hit")
    Event.connect("checkpoint_loaded", self, "reset")

func disconnect_signals():
  if not Engine.editor_hint:
    Event.disconnect("gem_collected", self, "_on_gem_collected")
    Event.disconnect("checkpoint_reached", self, "_on_checkpoint_hit")
    Event.disconnect("checkpoint_loaded", self, "reset")
  
func _on_gem_collected(col, position, frames):
  if self.color == col:
    current_state = COLLECTING
    self.animation = AnimatedSprite.new()
    animation.set_sprite_frames(frames)
    animation.play()
    add_child(animation)
    animation.global_position = position
    collected_animation = SlideAnimation.new(
      "gem_slide",
      animation,
      Vector2(20, 20),
      1)
    Event.connect("slide_animation_ended", self, "_on_slide_anim_ended", [], CONNECT_ONESHOT)

func _on_slide_anim_ended(anim_name):
  if anim_name == "gem_slide":
    if self.animation != null:
      self.remove_child(animation)
    if(current_state == COLLECTING): #this is normally the case unless the reset() was called
      $TextureRect.texture = texture_collected
      $TextureRect/AnimationPlayer.play("coin_collected_HUD")
      current_state = COLLECTED
    collected_animation = null
    
func _enter_tree():
  connect_signals()

func _exit_tree():
  disconnect_signals()
  
func _process(delta):
  if collected_animation != null:
    collected_animation.update(delta)

func reset():
  if should_reset_state:
    current_state = EMPTY
    if current_state == COLLECTED:
      $TextureRect.texture = texture_empty

func _on_checkpoint_hit(_checkpoint):
  if should_reset_state:
    if current_state == COLLECTED or current_state == COLLECTING:
      should_reset_state = false
