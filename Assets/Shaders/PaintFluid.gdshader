shader_type canvas_item;

// The liquid's surface, taken from how much paint is standing at each point rather than from any
// shape anybody drew. The simulation hands over a field - one number per cell saying how much
// paint is near it - and the surface is wherever that number crosses a threshold.
//
// This is what makes a crowd of particles read as one body of paint: a particle on its own raises
// the field enough to clear the threshold in a small circle, and two close together raise it
// between them as well, so their circles join into one shape with a smooth waist rather than
// showing up as two overlapping discs.
//
// The field carries two things. Red is the paint that is there now. Green is everywhere paint has
// been - what it left on the floor on its way past, which never washes off.

uniform sampler2D u_field : filter_linear, repeat_disable;

// Where the surface is, and how much of the field a pixel's worth of edge spans. The edge is what
// keeps the silhouette from being a staircase of field cells.
uniform float u_threshold = 0.5;
uniform float u_edge = 0.09;

// A stain is thinner than the paint that left it, so it takes less to count as covered.
uniform float u_stain_threshold = 0.3;

uniform vec4 u_color : source_color = vec4(1.0);

void fragment() {
  vec2 here = texture(u_field, UV).rg;

  float wet = smoothstep(u_threshold - u_edge, u_threshold + u_edge, here.r);
  float dry = smoothstep(u_stain_threshold - u_edge, u_stain_threshold + u_edge, here.g);
  float coverage = max(wet, dry);
  if (coverage <= 0.0) {
    discard;
  }

  // One flat colour, whether it is running or has dried. Shading the flowing paint by how deep it
  // is puts a dark underside on it that the stain it is lying on does not have, and the two read
  // as different materials meeting at a seam rather than as the same paint at two moments.
  COLOR = vec4(u_color.rgb, u_color.a * coverage);
}
