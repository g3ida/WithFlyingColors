shader_type canvas_item;

uniform sampler2D noise: hint_white;

void fragment(){ float speed_factor = TIME / 5.0;

float thickness_factor = 0.2; // float thickness_factor = (1.0 - pow(COLOR.a, 2));

vec2 fake_UV = vec2(abs(0.5 - UV.y) * thickness_factor, COLOR.a + speed_factor);

vec4 n = texture(noise, fake_UV);

float grey = 1.0 - (0.2126 * n.r + 0.7152 * n.g + 0.0722 * n.b);

if (grey < 0.55) { COLOR.a = COLOR.a * pow(grey, 2); }

if (grey < 0.5) { discard; } }