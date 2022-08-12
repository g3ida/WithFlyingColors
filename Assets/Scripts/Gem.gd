extends Area2D

export var group_name: String

const AMPLITUDE: float = 4.0
const ANIMATION_DURATION: float = 4.0

const NodeOcillator = preload("res://Assets/Scripts/Utils/NodeOcillator.gd")

var ocillator: NodeOcillator

func _ready():
	self.add_to_group(group_name)
	ocillator = NodeOcillator.new(self, AMPLITUDE, ANIMATION_DURATION)

func _physics_process(delta):
	ocillator.update(delta)

func _on_Gem_body_entered(body):
	self.queue_free()
