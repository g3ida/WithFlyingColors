namespace Wfc.Entities.World.Camera;

using Wfc.Utils;

// Everything a respawn has to put back, and nothing transient: no offset a shake was holding,
// no zoom a punch was mid-way through, no node a cutscene had borrowed the camera for.
//
// The member names are the save slot's contract - they are what an existing slot has written
// on disk - so rename them only together with the slots they have to keep reading.
public sealed record CameraFraming(
  float Zoom = 1f,
  int TopLimit = Constants.DEFAULT_CAMERA_LIMIT_TOP,
  int BottomLimit = Constants.DEFAULT_CAMERA_LIMIT_BOTTOM,
  int LeftLimit = Constants.DEFAULT_CAMERA_LIMIT_LEFT,
  int RightLimit = Constants.DEFAULT_CAMERA_LIMIT_RIGHT,
  float DragTopMargin = Constants.DEFAULT_DRAG_MARGIN_TB,
  float DragBottomMargin = Constants.DEFAULT_DRAG_MARGIN_TB,
  float DragLeftMargin = Constants.DEFAULT_DRAG_MARGIN_LR,
  float DragRightMargin = Constants.DEFAULT_DRAG_MARGIN_LR
);
