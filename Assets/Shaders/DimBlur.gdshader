shader_type canvas_item;

// Dims and softens whatever is already on screen, for UI that has to stay readable
// over the game without hiding it. Alpha is left alone, so fading the node's
// modulate cross-fades the whole effect in and out.
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;
uniform float radius : hint_range(0.0, 5.0) = 1.5;
uniform float black_factor : hint_range(0.0, 1.0) = 0.45;

void fragment() {
	COLOR.rgb = textureLod(SCREEN_TEXTURE, SCREEN_UV, radius).rgb * black_factor;
}
