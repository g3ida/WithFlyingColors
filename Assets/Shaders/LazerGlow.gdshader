shader_type canvas_item;
render_mode blend_add;

// The halo a fluorescent tube throws: additive light falling off smoothly from
// the tube, with the faint uneven flicker of a real gas lamp.
uniform float softness = 2.4;
uniform float flicker_strength : hint_range(0.0, 1.0) = 0.08;
uniform float flicker_speed = 11.0;

void fragment() {
	float across = abs(UV.y - 0.5) * 2.0;
	float glow = pow(max(1.0 - across, 0.0), softness);
	// Two incommensurate frequencies so the flicker never settles into a beat.
	float flicker = 1.0 - flicker_strength * (0.5 + 0.5 * sin(TIME * flicker_speed + sin(TIME * 27.0)));
	COLOR.a *= glow * flicker;
}
