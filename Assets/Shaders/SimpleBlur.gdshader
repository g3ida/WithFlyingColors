shader_type canvas_item;

uniform float radius: hint_range(0.0, 5) = 0.0;

void fragment(){
	vec4 color = texture(SCREEN_TEXTURE, SCREEN_UV, radius);
	COLOR = color;
}