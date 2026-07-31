shader_type canvas_item;
render_mode blend_add;

// The impact flare: a white-hot heart, a tight halo, and two layered sets of
// thin star rays that slowly rotate and shiver. Drawn procedurally so it stays
// crisp at any scale and needs no sprite sheet.
uniform float ray_count = 6.0;
uniform float ray_sharpness = 22.0;
uniform float rotation_speed = 0.35;
uniform float flicker_speed = 13.0;

// The sprite's modulate, read in the vertex stage before the texture multiply
// so the flare keeps the skin color without inheriting the texture's falloff.
varying vec4 tint;

void vertex() {
	tint = COLOR;
}

void fragment() {
	vec2 p = (UV - 0.5) * 2.0;
	float r = length(p);
	float angle = atan(p.y, p.x) + TIME * rotation_speed;

	// Two incommensurate frequencies so the flicker never settles into a beat.
	float flicker = 0.9 + 0.1 * sin(TIME * flicker_speed + sin(TIME * 31.0));

	float core = exp(-r * r * 16.0);
	float halo = 0.45 * exp(-r * 3.0);

	// |cos| gives ray_count lobes around the circle; the exponent thins each
	// lobe into a spike and the radial exponential fades it along its length.
	float rays = pow(abs(cos(angle * ray_count * 0.5)), ray_sharpness) * exp(-r * 2.2);
	// A shorter set offset half a step, so the star reads layered like a flare.
	float rays2 = 0.6 * pow(abs(cos(angle * ray_count * 0.5 + PI * 0.5)), ray_sharpness * 2.0) * exp(-r * 4.0);

	float breathing = 0.75 + 0.25 * sin(TIME * 5.0);
	float intensity = (core + halo + (rays + rays2) * breathing) * flicker;
	// Gone before the quad's edge, or the rays would end on a visible square.
	intensity *= 1.0 - smoothstep(0.8, 1.0, r);

	vec3 rgb = mix(tint.rgb, vec3(1.0), clamp(core * 1.4, 0.0, 1.0));
	COLOR = vec4(rgb, clamp(intensity, 0.0, 1.0) * tint.a);
}
