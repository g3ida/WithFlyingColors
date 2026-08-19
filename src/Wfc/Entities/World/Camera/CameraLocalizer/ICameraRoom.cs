namespace Wfc.Entities.World.Camera;

// One room of a level, as the camera sees it. Walked into with nothing else going on, a room takes
// the camera whole. A cutscene splits it in two, because the halves cannot land at the same moment:
// where the camera may go has to be in place before the shot starts its way home, so the leg
// absorbs the clamp instead of snapping into it on arrival, and what the camera shows has to wait
// until the leg is over, since a view changing under a clamped camera drags it off the curve.
public interface ICameraRoom {
  // How much of the world the room shows, for a shot opening in it to pull back to. Null for a
  // room with no view of its own to offer.
  float? Zoom { get; }

  void TakeTheCamera();

  // A room holds its zoom back until the pan its limits caused has been absorbed, which under a
  // shot has already happened: the way home was that pan. Answers with how long the change takes,
  // so a shot can hold its stripes in for it rather than guessing.
  float ShowTheRoom(bool aPanIsStillToCome);
}
