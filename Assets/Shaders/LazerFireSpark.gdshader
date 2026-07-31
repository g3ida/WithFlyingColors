shader_type canvas_item;
render_mode blend_add;

// One flying ember: a white-hot head inside a thin streak. The particle system
// aligns each quad's Y axis to its velocity, so the streak reads as a motion
// trail without any trail rendering.
uniform float streak_thinness = 50.0;
uniform float streak_length = 2.5;
uniform float head_tightness = 10.0;

// The particle's ramp color, read before the texture multiply so the spark's
// shape comes from this shader alone.
varying vec4 tint;

void vertex() {
	tint = COLOR;
}

void fragment() {
	vec2 p = (UV - 0.5) * 2.0;
	float body = exp(-p.x * p.x * streak_thinness) * exp(-p.y * p.y * streak_length);
	float head = exp(-dot(p, p) * head_tightness);
	float intensity = body + 0.8 * head;
	vec3 rgb = mix(tint.rgb, vec3(1.0), clamp(head * 1.3, 0.0, 1.0));
	COLOR = vec4(rgb, clamp(intensity, 0.0, 1.0) * tint.a);
}
