namespace Wfc.Entities.World.Platforms.Test;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Platforms;

// Telling a cube being carried by a platform apart from a cube the platform is driving into
// something. Both are a pair of overlapping rectangles, and every ride in the game overlaps by
// whatever the platform moved this frame - so the geometry has to answer this without leaning on
// how deep the overlap is, which is the one thing a crush does not reliably make bigger.
public class PlatformCrushTests(Node testScene) : TestClass(testScene) {
  private static readonly Rect2 CUBE = new Rect2(0f, 0f, 100f, 100f);
  private static readonly Vector2 DOWN = Vector2.Down;
  private static readonly Vector2 UP = Vector2.Up;
  private static readonly Vector2 RIGHT = Vector2.Right;

  // The cube is standing on a platform that is taking it down, dipped into the platform's top by
  // the distance the platform covered this frame. This is the whole of the brick breaker's lift
  // and none of it may kill.
  [Test]
  public void ACubeBeingCarriedIsBehindThePlatformAndSoNotCrushed() {
    var underfoot = new Rect2(-50f, 95f, 200f, 70f);

    PlatformCrush.PinchDepth(underfoot, CUBE, DOWN).ShouldBe(5f);
    PlatformCrush.IsAhead(underfoot, CUBE, DOWN).ShouldBeFalse();
    PlatformCrush.HasArrivedInto(underfoot, CUBE, DOWN).ShouldBeFalse();
  }

  // The cube is dead the moment it is between the two, not once the platform has made a meal of
  // it: every frame spent waiting for a deeper overlap is a frame the cube spends being carried
  // off through the floor.
  [Test]
  public void APlatformThatHasOnlyJustReachedTheCubeHasArrived() {
    var arriving = new Rect2(-50f, -68f, 200f, 70f);

    PlatformCrush.PinchDepth(arriving, CUBE, DOWN).ShouldBe(2f);
    PlatformCrush.HasArrivedInto(arriving, CUBE, DOWN).ShouldBeTrue();
  }

  // Which is only fair if the platform is actually overhead. A platform whose corner clips the
  // cube's is one the cube is standing beside, and it is deep into it along its own travel from
  // the very first frame of that.
  [Test]
  public void APlatformClippingTheCornerOfTheCubeHasNotArrived() {
    var offToTheSide = new Rect2(95f, -65f, 200f, 70f);

    PlatformCrush.PinchDepth(offToTheSide, CUBE, DOWN).ShouldBe(5f);
    PlatformCrush.Coverage(offToTheSide, CUBE, DOWN).ShouldBe(0.05f);
    PlatformCrush.HasArrivedInto(offToTheSide, CUBE, DOWN).ShouldBeFalse();
  }

  [Test]
  public void APlatformNowhereNearTheCubeHasNotArrived() {
    var elsewhere = new Rect2(300f, 0f, 200f, 70f);

    PlatformCrush.PinchDepth(elsewhere, CUBE, DOWN).ShouldBe(0f);
    PlatformCrush.HasArrivedInto(elsewhere, CUBE, DOWN).ShouldBeFalse();
  }

  [Test]
  public void APlatformComingDownOnTheCubeHasArrived() {
    var overhead = new Rect2(-50f, -65f, 200f, 70f);

    PlatformCrush.IsAhead(overhead, CUBE, DOWN).ShouldBeTrue();
    PlatformCrush.HasArrivedInto(overhead, CUBE, DOWN).ShouldBeTrue();
  }

  // The one configuration the geometry cannot settle on its own: a cube standing on a rising
  // platform is ahead of it, exactly like a cube being driven up into a ceiling. Which of the two
  // it is comes down to whether the cube has anywhere to go, so this states the arrival rather
  // than the crush - the escape below is what separates them.
  [Test]
  public void ACubeBeingLiftedLooksLikeAnArrivalAndIsLeftToTheEscape() {
    var underfoot = new Rect2(-50f, 95f, 200f, 70f);

    PlatformCrush.IsAhead(underfoot, CUBE, UP).ShouldBeTrue();
    PlatformCrush.HasArrivedInto(underfoot, CUBE, UP).ShouldBeTrue();
  }

  // Far enough to be out from under the platform, and no further: the cube is only crushed if it
  // cannot make this move, and asking for more room than it needs would kill it against a wall it
  // was never going to reach.
  [Test]
  public void TheEscapeClearsThePlatformAlongItsOwnTravel() {
    var overhead = new Rect2(-50f, -65f, 200f, 70f);

    PlatformCrush.EscapeMotion(overhead, CUBE, DOWN)
      .ShouldBe(new Vector2(0f, 5f + PlatformCrush.ESCAPE_MARGIN));
  }

  // The same two rectangles, and the answer is whichever way the platform is under power: a lick
  // of overlap across the top of the cube read the other way is a platform a third of the way
  // through it.
  [Test]
  public void DepthIsMeasuredAlongTheWayThePlatformTravels() {
    var platform = new Rect2(-170f, 90f, 200f, 200f);

    PlatformCrush.PinchDepth(platform, CUBE, DOWN).ShouldBe(10f);
    PlatformCrush.PinchDepth(platform, CUBE, RIGHT).ShouldBe(30f);
  }

  // The contact plane is the platform's leading edge, and it has to land on the platform's own
  // side of the cube's centre: everything downstream reads the side off it to know which way the
  // cube is being pressed, and a plane past the centre would flatten the cube into the crusher.
  [Test]
  public void TheContactPlaneIsTheLeadingEdgeOnTheSideThePlatformCameFrom() {
    var above = new Rect2(-50f, -65f, 200f, 70f);
    var below = new Rect2(-50f, 95f, 200f, 70f);

    PlatformCrush.ContactPoint(above, CUBE, DOWN).ShouldBe(new Vector2(50f, 5f));
    PlatformCrush.ContactPoint(below, CUBE, UP).ShouldBe(new Vector2(50f, 95f));
  }

  [Test]
  public void ASidewaysCrushReportsASidewaysContactPlane() {
    var fromTheLeft = new Rect2(-195f, -100f, 200f, 300f);
    var fromTheRight = new Rect2(95f, -100f, 200f, 300f);

    PlatformCrush.ContactPoint(fromTheLeft, CUBE, RIGHT).ShouldBe(new Vector2(5f, 50f));
    PlatformCrush.ContactPoint(fromTheRight, CUBE, Vector2.Left).ShouldBe(new Vector2(95f, 50f));
  }
}
