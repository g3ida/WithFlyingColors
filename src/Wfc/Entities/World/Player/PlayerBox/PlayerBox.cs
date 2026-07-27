namespace Wfc.Entities.World.Player;

using System;
using Godot;

// The cube's colored surface as plain numbers, in the cube's own frame and centered on the
// origin. Everything that has to decide which color the cube presents at a point asks this, so
// the surface is partitioned once rather than once per collision partner.
public readonly record struct PlayerBox(Vector2 HalfExtents, float CornerSeam) {
  // Which faces answer for a point. A corner is a seam the two faces it joins share, so a
  // contact there names both of them and either color is safe.
  [Flags]
  public enum Faces {
    None = 0,
    Right = 1,
    Left = 2,
    Bottom = 4,
    Top = 8,
  }

  // No faces at all means the point reaches none of them - it is inside the cube, or off it
  // altogether - which is not something the cube has an opinion about.
  public Faces FacesAt(Vector2 localPoint) {
    var faces = Faces.None;
    if (Mathf.Abs(localPoint.X) >= HalfExtents.X - CornerSeam) {
      faces |= localPoint.X >= 0.0f ? Faces.Right : Faces.Left;
    }
    if (Mathf.Abs(localPoint.Y) >= HalfExtents.Y - CornerSeam) {
      faces |= localPoint.Y >= 0.0f ? Faces.Bottom : Faces.Top;
    }
    return faces;
  }

  // A seam deeper than the cube leaves the faces between the corners with negative length, which
  // hands the color areas an inverted shape. The scale factor behind it is persisted in the
  // player's save data, so the bound has to hold for values no caller ever passes.
  public static float ClampSeam(float seam, Vector2 halfExtents) =>
    Mathf.Clamp(seam, 0.0f, Mathf.Min(halfExtents.X, halfExtents.Y));

  // How the color areas tile the perimeter: four corner squares, each pinned by its outer
  // corner so that widening a seam never changes the cube's silhouette, and four faces filling
  // what is left between them. `Overlap` is how far a face reaches past the join, which is what
  // stops a gap opening between the two for a contact to fall through.
  public readonly record struct Ring(float CornerOuterEdge, float RestingCornerSide, float Overlap) {
    private float RestingFaceHalfLength => CornerOuterEdge - RestingCornerSide + Overlap;

    public float RestingSeam(float halfExtent) => halfExtent - RestingFaceHalfLength;

    // Widening a corner square by `factor` about its pinned outer corner pushes the seam that
    // much further back along each of the two faces it joins.
    public float SeamFor(float factor, float halfExtent) =>
      RestingSeam(halfExtent) + (RestingCornerSide * (factor - 1.0f));

    public float FaceHalfLengthFor(float seam, float halfExtent) => halfExtent - seam;

    public float CornerSideFor(float faceHalfLength) => CornerOuterEdge + Overlap - faceHalfLength;
  }
}
