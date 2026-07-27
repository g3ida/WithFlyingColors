namespace Wfc.Entities.World.Player.Test;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Player;

// The one partition of the cube's surface. It used to be answered three different ways - off the
// color areas, off the body shapes, and analytically - and the three disagreed by up to three and
// a half times over exactly the region the corners exist to forgive.
public class PlayerBoxTests(Node testScene) : TestClass(testScene) {
  private static readonly Vector2 HALF = new Vector2(50f, 50f);
  private const float SEAM = 10f;

  private static PlayerBox _box(float seam = SEAM) => new PlayerBox(HALF, seam);

  [Test]
  public void TheMiddleOfAFaceNamesThatFaceAlone() {
    _box().FacesAt(new Vector2(0f, 50f)).ShouldBe(PlayerBox.Faces.Bottom);
    _box().FacesAt(new Vector2(0f, -50f)).ShouldBe(PlayerBox.Faces.Top);
    _box().FacesAt(new Vector2(50f, 0f)).ShouldBe(PlayerBox.Faces.Right);
    _box().FacesAt(new Vector2(-50f, 0f)).ShouldBe(PlayerBox.Faces.Left);
  }

  // The whole point of a corner: a contact on the seam is answered by both faces that meet
  // there, so either of their colors is survivable.
  [Test]
  public void AContactWithinTheSeamNamesBothFacesThatMeetThere() {
    _box().FacesAt(new Vector2(45f, 50f)).ShouldBe(PlayerBox.Faces.Right | PlayerBox.Faces.Bottom);
    _box().FacesAt(new Vector2(-45f, -50f)).ShouldBe(PlayerBox.Faces.Left | PlayerBox.Faces.Top);
  }

  [Test]
  public void JustInsideTheSeamNamesOnlyTheFaceItLandedOn() {
    _box().FacesAt(new Vector2(39f, 50f)).ShouldBe(PlayerBox.Faces.Bottom);
  }

  // Widening the seam is the whole of what "the corners are more forgiving now" means, and this
  // number is the only thing that says so. Nothing moves, nothing is rescaled.
  [Test]
  public void AWiderSeamForgivesFurtherFromTheCorner() {
    _box(seam: 5f).FacesAt(new Vector2(43f, 50f)).ShouldBe(PlayerBox.Faces.Bottom);
    _box(seam: 20f).FacesAt(new Vector2(43f, 50f)).ShouldBe(PlayerBox.Faces.Right | PlayerBox.Faces.Bottom);
  }

  // Inside the cube there is no surface and so no color. Callers read None as "nothing here has
  // an opinion", which is what keeps a ball that has climbed inside from being judged by whichever
  // face it happens to be nearest.
  [Test]
  public void APointInsideTheCubeNamesNoFace() {
    _box().FacesAt(Vector2.Zero).ShouldBe(PlayerBox.Faces.None);
  }

  // A contact is reported from wherever the other object's own geometry puts it, which is usually
  // outside the cube rather than exactly on it.
  [Test]
  public void APointOutsideTheCubeStillNamesTheFaceItIsOff() {
    _box().FacesAt(new Vector2(0f, 200f)).ShouldBe(PlayerBox.Faces.Bottom);
  }

  // The scale factor behind the seam is persisted in the player's save data, so the bound has to
  // hold for values no caller ever passes.
  [Test]
  public void ASeamCannotEatTheWholeCube() {
    PlayerBox.ClampSeam(1000f, HALF).ShouldBe(50f);
    PlayerBox.ClampSeam(-1f, HALF).ShouldBe(0f);
  }

  // The corner squares grow about their pinned outer corners and the faces give way to meet them,
  // so how forgiving the seam is never changes where the cube's surface is.
  [Test]
  public void GrowingTheCornersLeavesTheOuterEdgeWhereItWas() {
    var ring = new PlayerBox.Ring(CornerOuterEdge: 48f, RestingCornerSide: 3f, Overlap: 0f);
    var seam = ring.SeamFor(4f, 47f);
    var faceHalfLength = ring.FaceHalfLengthFor(seam, 47f);
    var cornerSide = ring.CornerSideFor(faceHalfLength);

    cornerSide.ShouldBe(ring.RestingCornerSide * 4f, 0.001f);
    (faceHalfLength + cornerSide).ShouldBe(ring.CornerOuterEdge, 0.001f);
  }

  // The faces overlap the corners rather than abutting them, which is what stops a contact
  // falling between the two as the seam widens.
  [Test]
  public void TheFacesMeetTheCornersWithoutOpeningAGap() {
    var ring = new PlayerBox.Ring(CornerOuterEdge: 48f, RestingCornerSide: 3f, Overlap: 0.1f);

    foreach (var factor in new[] { 1f, 3.5f, 4.5f, 10f }) {
      var seam = ring.SeamFor(factor, 47f);
      var faceHalfLength = ring.FaceHalfLengthFor(seam, 47f);
      var cornerInnerEdge = ring.CornerOuterEdge - ring.CornerSideFor(faceHalfLength);

      faceHalfLength.ShouldBeGreaterThan(cornerInnerEdge, $"a gap opens at scale {factor}");
    }
  }
}
