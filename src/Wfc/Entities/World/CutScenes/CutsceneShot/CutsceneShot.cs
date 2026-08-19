namespace Wfc.Entities.World.Cutscenes;

using Godot;
using Wfc.Entities.World.Camera;

// One borrow of the camera, as a level authors it: how long the shot holds before it moves, how
// wide it opens, how long it takes to get there and back, and on what curve. Passed whole rather
// than as a row of floats, which read the same at a call site and transpose silently.
public readonly record struct CutsceneShot(
  // How long the camera takes to reach the marker, and the same again on the way back.
  float TravelTime,
  // How long it rests on the marker once it has arrived, before it turns around.
  float HoldTime,
  // Held after the stripes are in and the player is locked, before the camera pulls back.
  float StartDelay,
  // How much of the world the shot shows. Zero opens on the room's own view, or on the camera's.
  float Zoom,
  CameraEasing Easing,
  // Where the curve spends its slow part: In leaves gently, Out settles onto the marker, InOut
  // does both.
  Tween.EaseType Ease
);
