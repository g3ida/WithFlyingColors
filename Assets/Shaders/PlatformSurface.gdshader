shader_type canvas_item;

// The surface every plain platform is drawn with: a flat colour rectangle with its corners
// sliced off at forty-five degrees and a short shade band down the two sides the light leaves
// dark. It is the silhouette the ground tileset bakes into its edge tiles, so a platform set
// against the ground reads as one continuous surface rather than a slab lying on top of it.
//
// The band is drawn inside the rectangle, not around it, which is the same trade the ground
// makes: the lit body gives up the two edges it shades. Clear an edge that a neighbour covers
// or its band shows up as a seam between the two.
//
// Everything here is measured in the node's own pixels, which only holds while the node is
// left unscaled - size a platform through u_size instead.

// Corners run top-left, top-right, bottom-right, bottom-left, matching the C# flags. Edges are
// right then bottom; the light comes from the top-left, so those are the only two that shade.
uniform vec2 u_size = vec2(64.0, 64.0);
uniform vec4 u_chamfer = vec4(4.0);
uniform vec2 u_shaded_edges = vec2(1.0, 1.0);
uniform float u_shade_width = 3.0;

// The landing splash, shared with ColorSplash.tres down to the uniform names, so PlatformSplash
// drives either one.
uniform float u_aspect_ratio = 1.0;
uniform float u_timer = -10.0;
uniform vec2 u_contact_pos = vec2(0.5, 0.5);
uniform float propagation_speed = 1.0;
uniform float darkness = 0.92;

// How much of the colour survives at the inner end of a band, before it ramps away to black. The
// light does not come in level: a side falls further into shadow than the underside does.
const float SHADE_NEAR_RIGHT = 0.79;
const float SHADE_NEAR_BOTTOM = 0.95;

// The shader's own fade term, kept in step with PlatformSplash: how much of the darkening the
// splash gives back per second.
const float SPLASH_FADE_RATE = 0.06;

const float INV_SQRT2 = 0.70710678;

// Writing COLOR wholesale throws away the node's modulate, so the tint is carried over from the
// vertex stage and multiplied back in by hand.
varying vec4 vertex_modulate;

void vertex() {
  vertex_modulate = COLOR;
}

// The shape is the four sides and the four corner cuts, each a half-plane, and a convex shape is
// the furthest of them; pushing one half-plane out grows the shape on that side alone, which is
// how a band is carved off the very same outline.
//
// The two halves are kept apart because only the corners are ever antialiased - see fragment().
// side_grow runs left, top, right, bottom. corner_grow follows chamfer.
float side_distance(vec2 p, vec2 size, vec4 side_grow) {
  vec2 from_far = size - p;
  return max(
    max(-p.x - side_grow.x, -from_far.x - side_grow.z),
    max(-p.y - side_grow.y, -from_far.y - side_grow.w)
  );
}

float corner_distance(vec2 p, vec2 size, vec4 chamfer, vec4 corner_grow) {
  vec2 from_far = size - p;
  float d = (chamfer.x - p.x - p.y) * INV_SQRT2 - corner_grow.x;
  d = max(d, (chamfer.y - from_far.x - p.y) * INV_SQRT2 - corner_grow.y);
  d = max(d, (chamfer.z - from_far.x - from_far.y) * INV_SQRT2 - corner_grow.z);
  d = max(d, (chamfer.w - p.x - from_far.y) * INV_SQRT2 - corner_grow.w);
  return d;
}

float sliced_rect_distance(vec2 p, vec2 size, vec4 chamfer, vec4 side_grow, vec4 corner_grow) {
  return max(side_distance(p, size, side_grow), corner_distance(p, size, chamfer, corner_grow));
}

// The expanding disc a landing leaves behind, in screen space: darkened inside, untouched
// outside, brightening back to nothing at a fixed rate.
float splash_brightness(vec2 screen_uv) {
  vec2 squash = vec2(1.0, u_aspect_ratio);
  vec2 here = (screen_uv - vec2(0.0, 0.5)) * squash;
  vec2 contact = (u_contact_pos - vec2(0.0, 0.5)) * squash;
  if (length(here - contact) < propagation_speed * u_timer) {
    return min(1.0, darkness + SPLASH_FADE_RATE * u_timer);
  }
  return 1.0;
}

void fragment() {
  vec2 shade = u_shade_width * u_shaded_edges;
  vec2 body_size = max(u_size - shade, vec2(1.0));
  vec2 p = UV * u_size;

  float body = sliced_rect_distance(p, body_size, u_chamfer, vec4(0.0), vec4(0.0));
  // One band per shaded edge, each taking the two corners it runs into with it. Together they
  // are the outline, so a corner is only chased by the band if an edge meeting it carries one.
  float right_band = sliced_rect_distance(
    p, body_size, u_chamfer, vec4(0.0, 0.0, shade.x, 0.0), vec4(0.0, shade.x, shade.x, 0.0)
  );
  float bottom_band = sliced_rect_distance(
    p, body_size, u_chamfer, vec4(0.0, 0.0, 0.0, shade.y), vec4(0.0, 0.0, shade.y, shade.y)
  );
  float outline = min(right_band, bottom_band);

  // Every distance comes off the same outline, so one screen-space width covers all of them.
  float aa = max(fwidth(body), 0.0001) * 0.5;

  // Alpha is taken off the corner cuts alone. The straight sides land exactly on the edges of the
  // quad, where the rasterizer already draws a hard edge - softening them as well leaves the
  // outermost column part transparent, and a platform butted against the ground shows the
  // background through the join every time the camera puts it between pixels.
  float corners = corner_distance(
    p, body_size, u_chamfer, vec4(0.0, shade.x, max(shade.x, shade.y), shade.y)
  );
  float coverage = 1.0 - smoothstep(-aa, aa, corners);

  // The body only fades into a band along an edge that carries one, which is the same seam again:
  // fading along an edge that carries none just darkens its outermost column.
  float has_band = clamp(body - outline, 0.0, 1.0);
  float lit = 1.0 - (smoothstep(-aa, aa, body) * has_band);

  // Around the corner where the two bands meet, cross from one shade to the other over the width
  // they overlap.
  float near = mix(SHADE_NEAR_BOTTOM, SHADE_NEAR_RIGHT, smoothstep(-1.0, 1.0, bottom_band - right_band));
  float ramp = 1.0 - smoothstep(0.0, 1.0, body / max(u_shade_width, 0.0001));
  float brightness = mix(near * ramp, 1.0, lit);

  COLOR = vec4(vertex_modulate.rgb * brightness * splash_brightness(SCREEN_UV), vertex_modulate.a * coverage);
}
