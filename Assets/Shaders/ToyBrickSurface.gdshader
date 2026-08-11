shader_type canvas_item;

// The surface a brick platform is drawn with, taken straight off the cell map the platform was
// authored as. Every cell says three things: which colour it wears, whether its piece is a brick
// standing on edge or a plate seen from above, and which ends of that piece it is. So where the
// studs stand, where the joins fall and which edges fall into shadow all follow the shape and the
// pieces the author painted - and the whole platform is drawn in one pass rather than a node to a
// brick.
//
// The map is padded by an empty cell on every side, so the studs standing on the top course have
// somewhere to be drawn. What a cell names is a palette slot rather than a colour, which keeps the
// surfaces exactly the colours the rest of the game takes from the skin.
//
// Everything here is measured in the node's own pixels, which only holds while the node is left
// unscaled - a platform is sized by its cell map and its cell size instead.

uniform vec2 u_size = vec2(96.0, 96.0);
uniform float u_cell = 32.0;
uniform vec2 u_cells = vec2(3.0, 3.0);
uniform sampler2D u_map : filter_nearest, repeat_disable;

// Slot 0 is an empty cell, and the five that follow are the four colour groups in the order
// ColorUtils declares them, then the neutral white the ground is drawn in.
uniform vec4 u_blue = vec4(1.0);
uniform vec4 u_pink = vec4(1.0);
uniform vec4 u_yellow = vec4(1.0);
uniform vec4 u_purple = vec4(1.0);
uniform vec4 u_neutral = vec4(1.0);

// The landing splash, shared with ColorSplash.tres down to the uniform names, so PlatformSplash
// drives this the same way it drives a flat platform.
uniform float u_aspect_ratio = 1.0;
uniform float u_timer = -10.0;
uniform vec2 u_contact_pos = vec2(0.5, 0.5);
uniform float propagation_speed = 1.0;
uniform float darkness = 0.78;

// Both are what the ground tileset bakes into its edge tiles, and PlatformSurface reproduces from
// the same numbers: a brick platform only reads as part of the same world while it is cut and
// shaded exactly as everything else is.
const float CHAMFER = 4.0;
const float SHADE_WIDTH = 3.0;
const float SHADE_NEAR_RIGHT = 0.79;
const float SHADE_NEAR_BOTTOM = 0.95;

const float SPLASH_FADE_RATE = 0.06;
const float INV_SQRT2 = 0.70710678;

// Which ends of its piece a cell carries, matching ToyBrickGrid.PieceEnds.
const float END_LEFT = 1.0;
const float END_RIGHT = 2.0;

// The groove where two bricks meet, and how much of the colour survives in it. The same line
// whichever way they meet: a course sitting on the next one is the same join as two bricks end to
// end, and a wall reads as a wall.
const float SEAM_HALF = 1.0;
const float SEAM_SHADE = 0.74;

// A stud on a brick seen from the side, as a share of the cell it stands on: how far it rises above
// the brick, how wide it is, how round its shoulders are, and how the light falls on it.
const float STUD_RISE = 0.2;
const float STUD_HALF_WIDTH = 0.26;
const float STUD_ROUND = 0.07;
const float STUD_TOP_LIGHT = 0.26;
const float STUD_SIDE_SHADE = 0.82;

// Which way a piece is drawn, matching ToyBrickGrid.PieceView.
const float VIEW_TOP = 1.0;
const float VIEW_HEAD = 2.0;

// The stud of a plate seen from above, standing in the middle of every cell of it.
const float TOP_STUD_RADIUS = 0.3;
const float TOP_STUD_SHADE = 0.93;
const float TOP_RIM_LIGHT = 1.22;
const float TOP_RIM_DARK = 0.7;

// A head, two studs each way. How far its corners are rounded off, where its eyes sit in its own
// frame and how big they are, and what is left of the colour inside one.
const float HEAD_ROUND = 0.44;
const float HEAD_EYE_HEIGHT = 0.95;
const float HEAD_EYE_SPREAD = 0.38;
const float HEAD_EYE_RADIUS = 0.16;
const float HEAD_EYE_SHADE = 0.07;

// Writing COLOR wholesale throws away the node's modulate, so the tint is carried over from the
// vertex stage and multiplied back in by hand.
varying vec4 vertex_modulate;

void vertex() {
  vertex_modulate = COLOR;
}

// What the map says about a cell: the palette slot, whether it is seen from above, and its piece
// ends. Everything outside the map is empty, which is what keeps the outermost bricks shaded and
// cut like the edge of a wall rather than like the middle of one.
vec3 cell_at(vec2 c) {
  if (any(lessThan(c, vec2(0.0))) || any(greaterThanEqual(c, u_cells))) {
    return vec3(0.0);
  }
  return texture(u_map, (c + 0.5) / u_cells).rgb;
}

vec3 slot_color(float slot) {
  if (slot < 1.5) {
    return u_blue.rgb;
  }
  if (slot < 2.5) {
    return u_pink.rgb;
  }
  if (slot < 3.5) {
    return u_yellow.rgb;
  }
  if (slot < 4.5) {
    return u_purple.rgb;
  }
  return u_neutral.rgb;
}

bool carries(float ends, float which) {
  return mod(floor(ends / which), 2.0) > 0.5;
}

// The shape is the four sides and the four corner cuts, each a half-plane, and a convex shape is
// the furthest of them; pushing one half-plane out grows the shape on that side alone, which is how
// a shade band is carved off the very same outline. Lifted from PlatformSurface, applied to one
// cell at a time.
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

// A box with rounded corners, grown on any side that carries on into a neighbour so that nothing is
// ever cut where two cells meet. Same trade the chamfer makes, with a curve instead of a cut.
float rounded_cell(vec2 p, vec2 size, vec4 reach, float radius) {
  vec2 centre = (size * 0.5) + vec2((reach.z - reach.x) * 0.5, (reach.w - reach.y) * 0.5);
  vec2 half_size = (size * 0.5) + vec2((reach.x + reach.z) * 0.5, (reach.y + reach.w) * 0.5);
  vec2 d = abs(p - centre) - half_size + vec2(radius);
  return min(max(d.x, d.y), 0.0) + length(max(d, vec2(0.0))) - radius;
}

// The expanding disc a landing leaves behind, in screen space: darkened inside, untouched outside,
// brightening back to nothing at a fixed rate.
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
  vec2 p = UV * u_size;
  vec2 grid = p / u_cell;
  vec2 cell = floor(grid);
  vec2 q = (grid - cell) * u_cell;

  vec3 here = cell_at(cell);
  float slot = here.r;
  vec3 tint;
  float brightness = 1.0;
  float coverage;

  if (slot > 0.5) {
    float left = cell_at(cell + vec2(-1.0, 0.0)).r;
    float right = cell_at(cell + vec2(1.0, 0.0)).r;
    float above = cell_at(cell + vec2(0.0, -1.0)).r;
    float below = cell_at(cell + vec2(0.0, 1.0)).r;

    // Only an edge with nothing beyond it is an edge of the platform. The rest are joins between
    // bricks, and a brick cut and shaded against its own neighbour reads as a hole in the wall.
    // Which way up a piece is seen changes what is drawn on its face and nothing about its
    // silhouette, so a platform of plates is cut and shaded exactly like a platform of bricks - and
    // two of them butted together show no background between them.
    bool open_left = left < 0.5;
    bool open_right = right < 0.5;
    bool open_top = above < 0.5;
    bool open_bottom = below < 0.5;

    if (here.g > VIEW_HEAD - 0.5) {
      // A head is one round face across the cells it covers, so its outline is a curve rather than
      // the brick's cut corners - held off only the edges with nothing beyond them, the same way,
      // so two cells of the same head never show the background between them.
      vec4 reach = vec4(
        open_left ? 0.0 : u_cell, open_top ? 0.0 : u_cell,
        open_right ? 0.0 : u_cell, open_bottom ? 0.0 : u_cell
      );
      float face = rounded_cell(q, vec2(u_cell), reach, u_cell * HEAD_ROUND);
      float head_aa = max(fwidth(face), 0.0001) * 0.5;
      coverage = 1.0 - smoothstep(-head_aa, head_aa, face);

      // Which quarter of the head this cell is, so the eyes are placed in the head's own frame
      // rather than in the cell's.
      vec2 quarter = vec2(mod(here.b, 2.0), floor(here.b * 0.5));
      vec2 on_head = q + (quarter * u_cell);
      float from_eye = min(
        length(on_head - vec2(u_cell * (1.0 - HEAD_EYE_SPREAD), u_cell * HEAD_EYE_HEIGHT)),
        length(on_head - vec2(u_cell * (1.0 + HEAD_EYE_SPREAD), u_cell * HEAD_EYE_HEIGHT))
      ) - (u_cell * HEAD_EYE_RADIUS);
      brightness = mix(1.0, HEAD_EYE_SHADE, 1.0 - smoothstep(-head_aa, head_aa, from_eye));
      tint = slot_color(slot);
    }
    else {
      vec2 shade = vec2(open_right ? SHADE_WIDTH : 0.0, open_bottom ? SHADE_WIDTH : 0.0);
      vec2 body_size = max(vec2(u_cell) - shade, vec2(1.0));
      vec4 chamfer = vec4(
        (open_left && open_top) ? CHAMFER : 0.0,
        (open_right && open_top) ? CHAMFER : 0.0,
        (open_right && open_bottom) ? CHAMFER : 0.0,
        (open_left && open_bottom) ? CHAMFER : 0.0
      );

      float body = sliced_rect_distance(q, body_size, chamfer, vec4(0.0), vec4(0.0));
      float right_band = sliced_rect_distance(
        q, body_size, chamfer, vec4(0.0, 0.0, shade.x, 0.0), vec4(0.0, shade.x, shade.x, 0.0)
      );
      float bottom_band = sliced_rect_distance(
        q, body_size, chamfer, vec4(0.0, 0.0, 0.0, shade.y), vec4(0.0, 0.0, shade.y, shade.y)
      );
      float outline = min(right_band, bottom_band);

      float aa = max(fwidth(body), 0.0001) * 0.5;

      // Alpha comes off the corner cuts alone. The straight sides land exactly on the edges of a cell,
      // where the rasterizer already draws a hard edge - softening those as well leaves the outermost
      // column part transparent, and every join between two bricks shows the background through it.
      float corners = corner_distance(
        q, body_size, chamfer, vec4(0.0, shade.x, max(shade.x, shade.y), shade.y)
      );
      coverage = 1.0 - smoothstep(-aa, aa, corners);

      float has_band = clamp(body - outline, 0.0, 1.0);
      float lit = 1.0 - (smoothstep(-aa, aa, body) * has_band);
      float near = mix(SHADE_NEAR_BOTTOM, SHADE_NEAR_RIGHT, smoothstep(-1.0, 1.0, bottom_band - right_band));
      float ramp = 1.0 - smoothstep(0.0, 1.0, body / max(SHADE_WIDTH, 0.0001));
      brightness = mix(near * ramp, 1.0, lit);

      // The joins this piece has: where it stops and the next one starts along the course, and
      // wherever a course sits on another. A groove is cut into both pieces that meet at it, so each
      // side draws its own half and the two make one line.
      float groove = 0.0;
      if (!open_left && carries(here.b, END_LEFT)) {
        groove = max(groove, 1.0 - smoothstep(SEAM_HALF - 0.5, SEAM_HALF + 0.5, q.x));
      }
      if (!open_right && carries(here.b, END_RIGHT)) {
        groove = max(groove, 1.0 - smoothstep(SEAM_HALF - 0.5, SEAM_HALF + 0.5, u_cell - q.x));
      }
      if (!open_top) {
        groove = max(groove, 1.0 - smoothstep(SEAM_HALF - 0.5, SEAM_HALF + 0.5, q.y));
      }
      if (!open_bottom) {
        groove = max(groove, 1.0 - smoothstep(SEAM_HALF - 0.5, SEAM_HALF + 0.5, u_cell - q.y));
      }
      brightness *= mix(1.0, SEAM_SHADE, groove);

      // Seen from above, the stud faces the viewer rather than the sky: a disc on the face of the
      // piece, lit on the side the light comes from and losing it on the other.
      if (here.g > VIEW_TOP - 0.5) {
        float stud = length(q - vec2(u_cell * 0.5)) - (u_cell * TOP_STUD_RADIUS);
        brightness *= mix(1.0, TOP_STUD_SHADE, 1.0 - smoothstep(-1.0, 1.0, stud));
        vec2 away = normalize((q - vec2(u_cell * 0.5)) + vec2(0.0001));
        float rim = mix(TOP_RIM_LIGHT, TOP_RIM_DARK, smoothstep(-1.0, 1.0, dot(away, vec2(INV_SQRT2))));
        brightness *= mix(1.0, rim, 1.0 - smoothstep(0.0, 1.6, abs(stud)));
        }

      tint = slot_color(slot);
    }
  }
  else {
    // Nothing is here, but the brick below may have its top free - and a brick with a free top has
    // studs on it, which stand in the cell above. A plate seen from above has none: its studs face
    // the viewer rather than the sky.
    vec3 stands_on = cell_at(cell + vec2(0.0, 1.0));
    if (stands_on.r < 0.5 || stands_on.g > VIEW_TOP - 0.5) {
      discard;
    }

    float rise = u_cell * STUD_RISE;
    float half_width = u_cell * STUD_HALF_WIDTH;
    float round_off = u_cell * STUD_ROUND;

    // A rounded box sitting on the cell's floor, so the stud meets the brick without a seam of its
    // own and only its shoulders are cut.
    vec2 from_centre = vec2(abs(q.x - (u_cell * 0.5)), u_cell - q.y);
    vec2 corner = vec2(half_width - round_off, rise - round_off);
    vec2 outside = max(from_centre - corner, vec2(0.0));
    float stud = length(outside) + min(max(outside.x, outside.y), 0.0) - round_off;
    // Only the shoulders need it: the floor of the cell is a hard edge the brick already draws to.
    float aa = max(fwidth(stud), 0.0001) * 0.5;
    coverage = 1.0 - smoothstep(-aa, aa, stud);

    // Lit across the crown, falling away at the foot and down the right, which is where the light
    // leaves every other surface in the game.
    float foot = 1.0 - smoothstep(0.0, rise, u_cell - q.y);
    float side = smoothstep(0.0, half_width, q.x - (u_cell * 0.5));
    brightness = mix(1.0 + STUD_TOP_LIGHT, STUD_SIDE_SHADE, max(foot, side));
    tint = slot_color(stands_on.r);
  }

  COLOR = vec4(
    vertex_modulate.rgb * tint * brightness * splash_brightness(SCREEN_UV),
    vertex_modulate.a * coverage
  );
}
