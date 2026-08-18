namespace Wfc.Entities.World.Camera;

// The curve a borrowed camera covers the ground on, between where it is and what it was sent to
// look at. The shot walks it over a time it fixes up front, which is what the engine's own
// smoothing cannot offer: that converges asymptotically and has no duration to be paced by.
//
// Back, Elastic and Bounce leave the leg they are on: the shot really does travel past what it was
// aimed at and come back. A room that clamps the axis they overshoot on absorbs that silently, so
// the same curve reads as characterful in an open room and as plain easing in a tight one - pick
// them for a leg with somewhere to overshoot into.
//
// Exported, so the order is serialized into every scene that sets it: append only.
public enum CameraEasing {
  Linear,
  Sine,
  Quad,
  Cubic,
  Quart,
  Quint,
  Expo,
  Circ,
  Back,
  Elastic,
  Bounce,
}
