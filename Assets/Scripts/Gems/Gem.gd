extends Area2D

export var group_name: String
export var sprite_frames: SpriteFrames

var states_store
var current_state: GemBaseState
var should_reset_state: bool = true

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
	set_light_color()
	$AnimatedSprite.frames = sprite_frames
	
	states_store = GemStatesStore.new(
		self,
		$Light2D,
		$AnimatedSprite,
		$AnimatedSprite/AnimationPlayer,
		$CollisionShape2D)
		
	current_state = states_store.not_collected
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
		var state = current_state.on_collision_with_body(area)
		switch_state(state)
		
func _on_AnimationPlayer_animation_finished(anim_name):
	var state = current_state.on_animation_finished(anim_name)
	switch_state(state)

func connect_signals():
	Event.connect("checkpoint_reached", self, "_on_checkpoint_hit")
	Event.connect("checkpoint_loaded", self, "reset")
		
func disconnect_signals():
	Event.disconnect("checkpoint_reached", self, "_on_checkpoint_hit")
	Event.disconnect("checkpoint_loaded", self, "reset")
				
func _enter_tree():
	connect_signals()
	
func _exit_tree():
	disconnect_signals()

func _on_checkpoint_hit(_checkpoint):
	if (current_state == states_store.collected) or (current_state == states_store.collecting):
		should_reset_state = false


func reset():
	if should_reset_state:
		switch_state(states_store.not_collected)