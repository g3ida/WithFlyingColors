shader_type canvas_item;
render_mode blend_mix;

uniform float radius = 10.0;

void fragment() {
	vec4 col = texture(SCREEN_TEXTURE, SCREEN_UV);
	vec2 ps = SCREEN_PIXEL_SIZE;
  
  if (radius > 0.1) {
   	col += texture(SCREEN_TEXTURE, SCREEN_UV + vec2(0.0, -radius) * ps);
   	col += texture(SCREEN_TEXTURE, SCREEN_UV + vec2(0.0, radius) * ps);
   	col += texture(SCREEN_TEXTURE, SCREEN_UV + vec2(-radius, 0.0) * ps);
   	col += texture(SCREEN_TEXTURE, SCREEN_UV + vec2(radius, 0.0) * ps);
      
   	col += texture(SCREEN_TEXTURE, SCREEN_UV + vec2(0.0, -radius*0.5) * ps);
   	col += texture(SCREEN_TEXTURE, SCREEN_UV + vec2(0.0, radius*0.5) * ps);
   	col += texture(SCREEN_TEXTURE, SCREEN_UV + vec2(-radius*0.5, 0.0) * ps);
   	col += texture(SCREEN_TEXTURE, SCREEN_UV + vec2(radius*0.5, 0.0) * ps);
      
   	col /= 9.0;
  }

	COLOR = col;
}