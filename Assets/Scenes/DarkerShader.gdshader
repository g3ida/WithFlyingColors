shader_type canvas_item;

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;
uniform float black_factor = 0.2;

// Blurs the screen in the Y-direction.
void fragment() {
    vec3 col = texture(SCREEN_TEXTURE, SCREEN_UV).xyz;
    col = col * vec3(black_factor, black_factor, black_factor);
    COLOR.xyz = col;
}