shader_type canvas_item;

uniform float black_factor = 0.2;

// Blurs the screen in the Y-direction.
void fragment() {
    vec3 col = texture(SCREEN_TEXTURE, SCREEN_UV).xyz;
    col = col * vec3(black_factor, black_factor, black_factor);
    COLOR.xyz = col;
}