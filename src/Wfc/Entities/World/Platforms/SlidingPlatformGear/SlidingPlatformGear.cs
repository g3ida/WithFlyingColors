namespace Wfc.Entities.World.Platforms;

using Godot;

// The cog on a sliding platform, turned by however far the platform has just travelled so that it
// reads as what is driving it.
[Tool]
public partial class SlidingPlatformGear : PlatformBadge {
  private const float SPIN_PER_PIXEL = 0.01f;

  // Turned by the platform rather than by watching it: the slider already knows how far it moved,
  // and a cog that reads its own position pays for that on every tick it stands still.
  public void Spin(float travelled) => Rotate(SPIN_PER_PIXEL * travelled);
}
