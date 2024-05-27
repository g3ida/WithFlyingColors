tool
extends Node2D

const SlideAnimation = preload("res://Assets/Scripts/Utils/SlideAnimation.cs")
const TextureCollected = preload("res://Assets/Sprites/HUD/gem_hud_collected.png")
const TextureEmpty = preload("res://Assets/Sprites/HUD/gem_hud.png")

onready var TextureRectNode = $TextureRect
onready var TextureRectAnimationNode = $TextureRect/AnimationPlayer
onready var BackgroundNode = $Background
onready var BackgroundAnimationPlayerNode = $Background/AnimationPlayer

export var color: String

enum {EMPTY, COLLECTING, COLLECTED}
var current_state = EMPTY

var save_data = {
  "state": current_state
}

var animation: AnimatedSprite
var collected_animation: SlideAnimation

# Called when the node enters the scene tree for the first time.
func _ready():
  TextureRectNode.texture = TextureEmpty
  BackgroundNode.visible = false
  var color_index = ColorUtils.get_group_color_index(color)
  TextureRectNode.modulate = ColorUtils.get_basic_color(color_index)
func connect_signals():
  if not Engine.editor_hint:
    var __ = Event.connect("gem_collected", self, "_on_gem_collected")
    __ = Event.connect("checkpoint_reached", self, "_on_checkpoint_hit")
    __ = Event.connect("checkpoint_loaded", self, "reset")

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
    animation.modulate = TextureRectNode.modulate
    add_child(animation)
    animation.set_owner(self)
    
    animation.global_position = position
    collected_animation = SlideAnimation.new()
    collected_animation.Set(
      "gem_slide",
      animation,
      Vector2(20, 20),
      1)
    var __ = Event.connect("slide_animation_ended", self, "_on_slide_anim_ended", [], CONNECT_ONESHOT)

func _on_slide_anim_ended(anim_name):
  if anim_name == "gem_slide":
    if self.animation != null:
      self.remove_child(animation)
    if(current_state == COLLECTING): #this is normally the case unless the reset() was called
      TextureRectNode.texture = TextureCollected
      TextureRectAnimationNode.play("coin_collected_HUD")
      BackgroundNode.visible = true
      BackgroundAnimationPlayerNode.play("coin_collected_HUD")
      current_state = COLLECTED
    collected_animation = null
    
func _enter_tree():
  connect_signals()

func _exit_tree():
  disconnect_signals()
  
func _process(delta):
  if collected_animation != null:
    collected_animation.Update(delta)

func reset():
  if save_data["state"] == EMPTY:
    TextureRectNode.texture = TextureEmpty
    BackgroundNode.visible = false
  else:
    TextureRectNode.texture = TextureCollected
    BackgroundNode.visible = true
    
func _on_checkpoint_hit(_checkpoint):
  save_data["state"] = current_state if current_state != COLLECTING else EMPTY

func save():
  return save_data
