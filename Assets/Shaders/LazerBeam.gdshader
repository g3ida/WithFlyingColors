shader_type canvas_item;

// A fluorescent tube: an even, near-solid body with a white-hot center line
// and only a faint traveling shimmer - the noise animates the energy without
// eating into the tube's clean silhouette.
uniform sampler2D noise : hint_default_white;
// How fast the energy pattern travels along the beam.
uniform float scroll_speed = 260.0;
uniform float noise_scale = 0.008;
// Fraction of the half-width that reads as the white-hot core.
uniform float core_width : hint_range(0.0, 1.0) = 0.55;

// Distance along the beam in local pixels. Line2D only generates a useful UV.x
// when it tiles a texture, so the position is carried out of the vertex stage
// instead.
varying float along;

void vertex() {
	along = VERTEX.x;
}

void fragment() {
	// 0.0 on the beam's center line, 1.0 at its edge.
	float across = abs(UV.y - 0.5) * 2.0;

	float phase = along * noise_scale - TIME * scroll_speed * noise_scale;
	float shimmer = texture(noise, vec2(phase, 0.31)).r;
	float energy = 0.85 + 0.15 * shimmer;

	// Solid through most of the width, feathered only at the rim.
	float body = 1.0 - smoothstep(0.7, 1.0, across);

	// The center line whitens into a hot core; squared so the falloff is tight.
	float core = 1.0 - smoothstep(0.0, core_width, across);
	core *= core;

	vec3 rgb = mix(COLOR.rgb, vec3(1.0), core * 0.95);
	float alpha = COLOR.a * body * energy;
	COLOR = vec4(rgb, clamp(alpha, 0.0, 1.0));
}
