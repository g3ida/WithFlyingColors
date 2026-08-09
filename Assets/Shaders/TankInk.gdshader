shader_type canvas_item;

// The ink standing in the gun's tank. The tank turns with the gun, but what is in it does not: a
// liquid's surface is level whatever is holding it, so the cut has to be made in the world's
// upright rather than in the sprite's own - clipping the sprite's own height tips the ink over with
// the barrel and reads as a solid block painted inside it.

// The height the ink comes up to, in the same space the gun is drawn in. Everything below is ink.
uniform float u_level = 0.0;

varying vec2 world;

void vertex() {
  world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

void fragment() {
  if (world.y < u_level) {
    discard;
  }
  COLOR = texture(TEXTURE, UV) * COLOR;
}
