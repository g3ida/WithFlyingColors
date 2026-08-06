namespace Wfc.Entities.World.Camera;

using System;

// The sides of a room that hold the camera in. The four are independent, and a side left out is
// opened up instead: the camera travels past it as far as anything else lets it.
[Flags]
public enum CameraEdges {
  Left = 1,
  Right = 2,
  Top = 4,
  Bottom = 8,
}
