tool
extends Node2D

const SlideAnimation = preload("res://Assets/Scripts/Utils/SlideAnimation.gd")

export var texture_collected: Texture
export var texture_empty: Texture
export var color: String

var animation: AnimatedSprite
var collected_animation: SlideAnimation

# Called when the node enters the scene tree for the first time.
func _ready():
	$TextureRect.texture = texture_empty
func connect_signals():
	if not Engine.editor_hint:
		Event.connect("gem_collected", self, "_on_gem_collected")

func disconnect_signals():
	if not Engine.editor_hint:
		Event.disconnect("gem_collected", self, "_on_gem_collected")
	
func _on_gem_collected(col, position, frames):
	if self.color == col:
		$TextureRect.texture = texture_collected
		self.animation = AnimatedSprite.new()
		animation.set_sprite_frames(frames)
		animation.play()
		add_child(animation)
		animation.global_position = position - Global.HUD_offset
		collected_animation = SlideAnimation.new(
			"gem_slide",
			animation,
			Vector2(32, 32),
			1000)
		Event.connect("slide_animation_ended", self, "_on_slide_anim_ended", [], CONNECT_ONESHOT)

func _on_slide_anim_ended(anim_name):
	if anim_name == "gem_slide":
		if self.animation != null:
			self.remove_child(animation)
		$TextureRect/AnimationPlayer.play("coin_collected_HUD")
		collected_animation = null
		
func _enter_tree():
	connect_signals()

func _exit_tree():
	disconnect_signals()
	
func _process(delta):
	if collected_animation != null:
		collected_animation.update(delta)
