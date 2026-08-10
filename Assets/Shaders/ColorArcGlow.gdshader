shader_type canvas_item;
render_mode blend_add;

// The halo the arc sits inside. Additive and soft all the way across, so it reads
// as light spilling off the discharge rather than a second, fatter line - the ColorArc
// pass underneath it is what carries the shape.

uniform float falloff : hint_range(0.5, 6.0) = 1.9;
uniform float flicker_speed = 41.0;
uniform float flicker_depth : hint_range(0.0, 1.0) = 0.45;

void fragment() {
	float across = abs(UV.y - 0.5) * 2.0;

	float halo = pow(1.0 - clamp(across, 0.0, 1.0), falloff);

	float a = sin(TIME * flicker_speed);
	float b = sin(TIME * flicker_speed * 1.71 + 1.3);
	float energy = 1.0 - flicker_depth * (0.5 - 0.5 * a * b);

	COLOR = vec4(COLOR.rgb, clamp(COLOR.a * halo * energy, 0.0, 1.0));
}
