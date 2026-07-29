shader_type canvas_item;

// Squeezes the cube into a bullet pointing the way it is travelling: the leading cap rounds
// forward, the tail is scooped out behind it, the sides are drawn in at the back and pushed
// out across the middle.
//
// It is written as an inverse map - for the pixel being drawn, work out which part of the
// sprite ends up there - so the shape is redistributed inside the sprite's own quad and
// nothing can be pushed past the edge and cut off.

uniform vec2 dash_dir = vec2(1.0, 0.0);
uniform float strength : hint_range(0.0, 1.0) = 0.0;

// One knob over the whole silhouette. Turn it down for a subtler bullet without touching the
// hot rim or the tail fade; 0 leaves the cube square and everything else intact.
uniform float shape : hint_range(0.0, 1.5) = 0.6;

uniform float cap : hint_range(0.0, 0.4) = 0.09;
uniform float scoop : hint_range(0.0, 0.4) = 0.14;
uniform float taper : hint_range(0.0, 0.6) = 0.36;
uniform float belly : hint_range(0.0, 0.4) = 0.16;

uniform float glow : hint_range(0.0, 1.0) = 0.5;
uniform float tail_fade : hint_range(0.0, 1.0) = 0.35;

// Writing COLOR wholesale throws away the node's modulate, which the dying states tween, so
// it is carried over from the vertex stage and multiplied back in by hand.
varying vec4 vertex_modulate;

void vertex() {
  vertex_modulate = COLOR;
}

// How much silhouette shaping is in play this frame: the dash's own ramp, scaled by the one
// knob that says how bullet-like the cube should ever get.
float morph() {
  return strength * shape;
}

// How far forward the nose reaches at this distance off the axis. It stays put dead centre
// and is pulled back towards the corners, which is what rounds it.
float leading_edge(float across) {
  return 0.5 - cap * morph() * clamp(4.0 * across * across, 0.0, 1.0);
}

// The mirror of the nose: pulled forward in the middle and left alone at the corners, so the
// back of the cube caves in.
float trailing_edge(float across) {
  return -0.5 + scoop * morph() * (1.0 - clamp(4.0 * across * across, 0.0, 1.0));
}

float half_width(float along) {
  float pinch = taper * morph() * clamp(0.5 - along, 0.0, 1.0);
  float bulge = belly * morph() * (1.0 - clamp(4.0 * along * along, 0.0, 1.0));
  // Pull the whole width back in by the most the belly can ever add, so the widest point of
  // the shape lands exactly on the edge of the quad instead of outside it.
  return max(0.2, (1.0 - pinch + bulge) / (1.0 + belly * morph()));
}

void fragment() {
  vec2 forward = normalize(dash_dir);
  vec2 side = vec2(-forward.y, forward.x);

  // A diagonal dash points at a corner of the sprite, which is further from the middle than a
  // side is. Measuring both axes against how far the quad actually reaches along them puts a
  // diagonal dash on the same footing as a flat one, instead of deforming it half as much and
  // kinking the silhouette where the shape functions run past the range they expect.
  float along_reach = abs(forward.x) + abs(forward.y);
  float across_reach = abs(side.x) + abs(side.y);

  vec2 centered = UV - 0.5;
  float along = dot(centered, forward) / along_reach;
  float across = dot(centered, side) / across_reach;

  // The width depends on how far along the shape we are and how far along we are depends on
  // the width, so the two are solved against each other. One correction is plenty at these
  // amplitudes.
  float src_across = across;
  float src_along = along;
  for (int i = 0; i < 2; i++) {
    float lead = leading_edge(src_across);
    float tail = trailing_edge(src_across);
    src_along = (along - tail) / max(lead - tail, 0.001) - 0.5;
    src_across = across / half_width(src_along);
  }

  vec2 uv = 0.5 + (src_along * along_reach * forward) + (src_across * across_reach * side);
  // At rest the map is the identity, but rounding can still put a corner a hair outside the
  // quad, and discarding there would leave the resting cube missing a pixel off its edge.
  bool outside = uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0;
  if (morph() > 0.0 && outside) {
    discard;
  }
  uv = clamp(uv, vec2(0.0), vec2(1.0));

  // One sample, deliberately. Dragging the sprite forward over itself only ever averaged texels
  // that were already on it - taps landing off it were weighted out, so it never widened the
  // silhouette - and what it did instead was blend the four faces into each other. Which face
  // shows where is the thing the player reads the cube by, so it stays sharp; the speed lines
  // and the bullet silhouette are what say the cube is moving.
  vec4 col = texture(TEXTURE, uv);

  // A hot rim at the nose and a tail that thins out into the trail behind it.
  col.rgb = mix(col.rgb, vec3(1.0), glow * strength * smoothstep(0.15, 0.5, along));
  col.a *= 1.0 - tail_fade * strength * (1.0 - smoothstep(-0.5, -0.15, along));

  COLOR = col * vertex_modulate;
}
