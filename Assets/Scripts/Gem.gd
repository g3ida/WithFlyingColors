extends Area2D

export var group_name: String
export var sprite_frames: SpriteFrames

const AMPLITUDE: float = 4.0
const ANIMATION_DURATION: float = 4.0
const SHINE_VARIANCE = 0.08
const ROTATION_SPEED = 0.002

const NodeOcillator = preload("res://Assets/Scripts/Utils/NodeOcillator.gd")

var ocillator: NodeOcillator

func set_light_color():
	if group_name == 'blue':
		$Light2D.color =  Color( 0, 0.9215, 1, 1 )
	elif group_name == 'pink':
		$Light2D.color =  Color( 1, 0, 0.5215, 1 )
	elif group_name == 'yellow':
		$Light2D.color =  Color( 0.8, 1, 0, 1 )
	elif group_name == 'purple':
		$Light2D.color =  Color( 0.635, 0.035, 0.964, 1 )
	else:
		push_error("unknown color group : " + group_name)
		
func _ready():
	self.add_to_group(group_name)
	ocillator = NodeOcillator.new(self, AMPLITUDE, ANIMATION_DURATION)
	set_light_color()
	$AnimatedSprite.frames = sprite_frames

func _physics_process(delta):
	ocillator.update(delta)
	$Light2D.energy = 1 + SHINE_VARIANCE * sin(2 * PI * ocillator.timer / ANIMATION_DURATION)
	$Light2D.rotate(ROTATION_SPEED)

func _on_Gem_body_entered(body):
	self.queue_free()
