namespace Wfc.Entities.World.Platforms;

using Godot;

// The curved arrow on a rotating platform. It is the one thing that says which way a surface is
// about to go, which on a platform that turns is the difference between a ride and a fall.
[Tool]
public partial class RotatingPlatformArrow : PlatformBadge {
  // Where the arrow is pointed from, rather than by watching the platform: the platform already
  // knows which leg of its cycle it is on, and it knows it while it is still standing at the end of
  // the last one - so the arrow has turned round before the platform does.
  //
  // The art is drawn turning the way a clock does, and only mirroring it turns it the other way -
  // which is why the arrow is left to ride round with the surface it is drawn on. Turning a curved
  // arrow through any angle at all leaves it pointing the same way round.
  public void Point(float heading) {
    if (!Mathf.IsZeroApprox(heading)) {
      FlipH = heading < 0.0f;
    }
  }
}
