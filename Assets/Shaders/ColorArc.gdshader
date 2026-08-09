shader_type canvas_item;

// One wire of a discharge arc. The jagged path is the line's own geometry, so all
// this does is turn a flat ribbon into something that reads as energy: a bright
// center inside a feathered sheath, flickering hard enough to look unstable.
//
// It brightens toward the center rather than whitening. The two colors the wire
// runs between are the whole point of it - which face is charged and what it is
// charged against - and a white-hot core says neither.

// Fraction of the half-width that reads as the bright core.
uniform float core_width : hint_range(0.0, 1.0) = 0.5;
// How much brighter the core burns than the color the gradient hands it.
uniform float core_gain : hint_range(1.0, 4.0) = 1.9;
uniform float flicker_speed = 41.0;
uniform float flicker_depth : hint_range(0.0, 1.0) = 0.3;

void fragment() {
	// 0.0 on the center line, 1.0 at the edge. Line2D only emits this when its
	// texture_mode is STRETCH - with the default NONE every UV is (0,0) and the
	// line disappears without an error.
	float across = abs(UV.y - 0.5) * 2.0;

	float body = 1.0 - smoothstep(0.45, 1.0, across);

	float core = 1.0 - smoothstep(0.0, core_width, across);
	core *= core;

	// Two rates that never line up, so the flicker keeps no audible beat.
	float a = sin(TIME * flicker_speed);
	float b = sin(TIME * flicker_speed * 1.71 + 1.3);
	float energy = 1.0 - flicker_depth * (0.5 - 0.5 * a * b);

	vec3 rgb = COLOR.rgb * (1.0 + ((core_gain - 1.0) * core));
	COLOR = vec4(rgb, clamp(COLOR.a * body * energy, 0.0, 1.0));
}
