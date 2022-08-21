shader_type canvas_item;

uniform float hit_opacity = 0;
uniform bool is_animating = true;


void fragment(){
  vec4 col = texture(TEXTURE, UV).rgba;
  if (!is_animating)
    COLOR.rgb = col.rgb * vec3(0.176, 0.176, 0.176);
  else
    COLOR.rgb = col.rgb;
  COLOR.a = texture(TEXTURE, UV).a * (1.0 - hit_opacity);
}