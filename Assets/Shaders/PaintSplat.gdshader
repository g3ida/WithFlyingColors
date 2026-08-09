shader_type canvas_item;

// The paint a dropped bucket leaves behind, and the coat an inked platform wears: a pool lying
// along the top of whatever it is on, thinning to nothing at both ends where it was spilt, with
// fingers of it running off the underside.
//
// Nothing here is drawn from a texture. The ends of the pool and every finger are cut from one
// hash of the finger's own index, so two splats of the same width never land the same way and a
// wide one never repeats itself - which is what stretching or tiling the bucket's own drip
// sprite along a platform would have done.
//
// Everything is measured in the node's own pixels with the top edge of the quad on the surface
// the paint landed on, so the node is placed by its top edge and left unscaled - size it through
// u_size instead.

uniform vec2 u_size = vec2(256.0, 150.0);

// How deep the pool lies over the surface at the point the bucket hit, and how far past it the
// longest finger can run.
uniform float u_pool = 40.0;
uniform float u_reach = 110.0;

// The throw and then the run: how far the paint has spread out from the point of impact, and how
// far the fingers have got down. Both 0..1, both animated by PaintSplat.
uniform float u_spread = 1.0;
uniform float u_run = 1.0;

// Which splat this is. Two buckets emptied over the same platform draw the same paint otherwise.
uniform float u_seed = 0.0;

// Whether the paint runs out at its own ends or reaches the ends of what it is on. Spilt paint
// thins away to nothing wherever it stopped; a coat someone painted goes right to the edge of the
// platform and stops there with it.
uniform float u_ends : hint_range(0.0, 1.0) = 1.0;

uniform vec4 u_color : source_color = vec4(1.0);
uniform vec4 u_shade : source_color = vec4(0.8, 0.8, 0.8, 1.0);

// One finger per this much of the paint's length. The fingers are placed off the spacing rather
// than at random along the whole width, which is what keeps them from clumping.
uniform float u_spacing = 26.0;

// What a finger's width is measured against. Held apart from the spacing so that setting paint to
// run in more fingers gives it more of the same fingers rather than the same number of fatter
// ones - how many there are and how thick they are being two different things about a coat.
uniform float u_width = 26.0;

// The share of that width a finger is across, thinnest to thickest.
const float FINGER_RADIUS_MIN = 0.17;
const float FINGER_RADIUS_MAX = 0.33;

// How far off its own cell's centre a finger may sit.
const float FINGER_JITTER = 0.62;

// The most cells either side that are asked about. Fingers wide against their spacing reach past
// their neighbours, and a finger left out of the sum is one cut off down its side; the count is
// worked out from the two, and this is only what it may not exceed.
const int FINGER_REACH_MAX = 6;

// How much a finger may swell or pinch partway down, and how far off plumb it may hang. Both are
// small on purpose: paint runs downhill, and a finger with any real angle on it reads as blown
// sideways rather than as paint.
const float FINGER_SWELL = 0.3;
const float FINGER_LEAN = 0.07;

// How far below the pool a finger takes on its own rounding. It leaves the pool as part of the
// same face and is a column of its own only once it is clear of it, so the two shadings meet as
// one surface rather than as two laid against each other.
const float FINGER_ROUND_IN = 10.0;

// How much wider a finger is where it hangs from the pool, and how quickly it draws back in.
// Paint is pulled out of the pool rather than cut from it, so it leaves in a curve rather than
// at the corner a column of even width meets the underside in.
const float FINGER_FLARE = 0.45;
const float FINGER_FLARE_FALL = 11.0;

// How far into the shade colour a fully shaded pixel goes. The paint keeps most of its own colour
// even where it turns away from the light.
const float SHADE_DEPTH = 1.0;

float hash(float n) {
  return fract(sin(n * 127.1 + u_seed * 13.7) * 43758.5453123);
}

// Which of the two ends a point belongs to. Paint does not land in a mirror image of itself, so
// each end gets its own throw out of the hash: how far it reached, how full it stayed on the way,
// and how quickly it ran out at the last.
float side_of(float x) {
  return x < 0.0 ? hash(23.0) : hash(29.0);
}

// How far the paint reached on the end x lies on.
float side_span(float x, float reach) {
  return reach * (0.74 + (0.26 * side_of(x)));
}

// The pool's underside, as a depth below the surface at x. It is deepest over the point of impact
// and runs out to nothing at the spread, with a slow wobble along it so the edge does not read as
// a drawn curve.
float pool_depth(float x, float reach) {
  float side = side_of(x);
  float t = clamp(abs(x) / max(side_span(x, reach), 0.001), 0.0, 1.0);
  // Thrown paint keeps its depth most of the way out and then runs out of itself, rather than
  // thinning evenly from the middle the way a drawn lens would. The last of it closes off inside
  // the quad: a pool still deep where the quad ends is cut off square, and a straight vertical
  // edge is the one thing in the whole splat that reads as drawn rather than spilt.
  float run_out = pow(max(1.0 - t * t, 0.0), 0.32 + (0.2 * side)) * (1.0 - smoothstep(0.68 + (0.2 * side), 0.99, t));
  float body = mix(1.0, run_out, u_ends);
  // Three lengths of wobble laid over each other, the slowest carrying most of it: a pool whose
  // underside runs level is a band with a fringe on it rather than paint that gathered where it
  // gathered. The range is wide on purpose - the deepest stretches are twice the shallowest.
  float wobble = 0.72
    + 0.15 * sin((x * 0.021) + (hash(3.0) * 6.283))
    + 0.08 * sin((x * 0.062) + (hash(7.0) * 6.283))
    + 0.05 * sin((x * 0.148) + (hash(11.0) * 6.283));
  return u_pool * body * wobble;
}

// Coverage, shade and attachment of the fingers at p. A finger is a rounded column hanging from
// under the pool, narrowing as it runs and swelling again just before its tip, which is what
// makes falling paint read as falling rather than as a set of tapered spikes.
vec3 fingers(vec2 p, float reach, float aa) {
  float id = floor(p.x / u_spacing);
  float coverage = 0.0;
  float shade = 0.0;
  float attachment = 0.0;

  // How far the widest finger drawn from a cell can carry past that cell, in cells.
  float widest = u_width * FINGER_RADIUS_MAX * (1.0 + FINGER_FLARE);
  int cells = int(min(
    ceil((widest / max(u_spacing, 0.001)) + 0.5 + (0.5 * FINGER_JITTER)),
    float(FINGER_REACH_MAX)
  ));

  for (int k = -cells; k <= cells; k++) {
    float i = id + float(k);
    float cx = (i + 0.5 + (FINGER_JITTER * (hash(i) - 0.5))) * u_spacing;

    // A finger needs pool above it to run out of, so the short ones are at the ends where there
    // is barely any pool left, and past the end of the pool there is no finger at all - one drawn
    // there hangs off nothing, as a nub standing proud of the surface the paint landed on.
    float t = clamp(abs(cx) / max(side_span(cx, reach), 0.001), 0.0, 1.0);
    float carry = mix(
      1.0,
      sqrt(max(1.0 - t * t, 0.0)) * (1.0 - smoothstep(0.68 + (0.2 * side_of(cx)), 0.99, t)),
      u_ends
    );
    float length_here = u_reach * (0.14 + 0.86 * hash(i + 17.0)) * carry * u_run;
    float radius = u_width * mix(FINGER_RADIUS_MIN, FINGER_RADIUS_MAX, hash(i + 41.0)) * min(carry * 2.0, 1.0);
    // No two of them run the same way down: one swells partway along where it is carrying more
    // than it can hold and the next is pinched, and none of them hangs quite plumb.
    float swell = (hash(i + 5.0) - 0.5) * 2.0 * FINGER_SWELL;
    float lean = (hash(i + 63.0) - 0.5) * 2.0 * FINGER_LEAN;

    // Hung from inside the pool, but leaving it at its underside: that is where it flares.
    float attach = pool_depth(cx, reach);
    float top = attach * 0.7;
    float run = clamp((p.y - top) / max(length_here, 0.001), 0.0, 1.0);
    // Thinning as it runs and barely gathering again at the end, so a finger ends in a rounded tip
    // rather than the head of a mushroom - a tip much wider than the run above it reads as a blob
    // stuck on the end, and worst on the thinnest fingers where the contrast is greatest.
    float width = radius * (1.0 - (0.28 * run) + (0.22 * smoothstep(0.62, 1.0, run)));
    width *= 1.0 + (swell * sin(run * 3.1416));
    width *= 1.0 + (FINGER_FLARE * exp(-max(p.y - attach, 0.0) / FINGER_FLARE_FALL));

    float beyond = max(p.y - (top + length_here), 0.0);
    float centre = cx + (lean * max(p.y - top, 0.0));
    float distance_here = length(vec2(p.x - centre, beyond)) - width;

    float here = 1.0 - smoothstep(-aa, aa, distance_here);
    coverage = max(coverage, here);
    shade = max(shade, here * smoothstep(-0.2, 0.85, (p.x - centre) / max(width, 0.001)));
    // Whether a finger hangs below this x at all, whether or not the pixel itself is on one.
    // Measured against the flared width, since that is what the pool's underside gives way to.
    float base = radius * (1.0 + FINGER_FLARE);
    attachment = max(
      attachment,
      smoothstep(0.0, 2.0, length_here) * (1.0 - smoothstep(base * 0.5, base * 1.5, abs(p.x - cx)))
    );
  }

  return vec3(coverage, shade, attachment);
}

void fragment() {
  vec2 p = vec2((UV.x - 0.5) * u_size.x, UV.y * u_size.y);
  float reach = u_size.x * 0.5 * u_spread;
  float aa = max(0.5 * length(vec2(fwidth(p.x), fwidth(p.y))), 0.001);

  float depth = pool_depth(p.x, reach);
  // Past where the pool has run out its depth is a fraction of a pixel rather than none, and the
  // top row of the quad half-covered from end to end is a hairline drawn across the surface.
  float pool = (1.0 - smoothstep(-aa, aa, p.y - depth)) * smoothstep(0.0, 1.5, depth);
  vec3 run = fingers(p, reach, aa);

  float coverage = max(pool, run.x);
  // The light comes from the top-left, the same way it does for every platform: the pool darkens
  // towards its underside and each finger down its right-hand side. The pool is only shaded where
  // it has an underside - where a finger hangs off it the paint carries on down, and an edge
  // shaded across a face that does not end there draws a line along the top of every finger.
  float lip = smoothstep(depth * 0.4, depth, p.y) * (1.0 - run.z);
  float shade = mix(lip, run.y, smoothstep(0.0, FINGER_ROUND_IN, p.y - depth));

  COLOR = vec4(mix(u_color.rgb, u_shade.rgb, shade * SHADE_DEPTH), u_color.a * coverage);
}
